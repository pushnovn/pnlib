using IBM.WMQ;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PN.Network.IBM
{
    public class MQ
    {
        #region Settings

        private static Settings CurrentSettings = new Settings();

        public class Settings
        {
            public string QueueName { get; set; }
            public string QueueManagerName { get; set; }
            public string Channel { get; set; }
            public string Host { get; set; }
            public string Port { get; set; }
            public string UserId { get; set; }
            public string Password { get; set; }
        }

        public static void ExtractDll(bool rewriteFileIfExists = true)
        {
            if (File.Exists("amqmdnet.dll") && rewriteFileIfExists == false)
                return;

            try { File.Delete("amqmdnet.dll"); }
            catch { }

            Utils.Internal.WriteResourceToFile("PN.Network.IBM.amqmdnet.dll", "amqmdnet.dll");
        }

        #endregion


        #region Constants

        private const int SOME_MAGIC_EXCEPTION_VALUE = -532462766;
        private const int MQ_EXCEPTION_EMPTY_QUEUE = 2033;
        private const int MQ_EXCEPTION_ALREADY_CONNECTED = 2002;

        private const int QUEUE_GET_OPEN_OPTIONS = MQC.MQOO_FAIL_IF_QUIESCING + MQC.MQOO_INPUT_AS_Q_DEF;
        private const int QUEUE_PUT_OPEN_OPTIONS = MQC.MQOO_FAIL_IF_QUIESCING + MQC.MQOO_OUTPUT;

        #endregion


        #region Get & Put methods

        public static string Get(string queueName) => ReadWriteMessageInQueue(queueName);
        public static string Get(string queueName, out long evaluateTime)
            => ReadWriteMessageInQueue(queueName, null, out evaluateTime);

        public static void Put(string queueName, string message)
            => ReadWriteMessageInQueue(queueName, message ?? throw new Exception("Message cannot be null."));
        public static void Put(string queueName, string message, out long evaluateTime)
            => ReadWriteMessageInQueue(queueName, message ?? throw new Exception("Message cannot be null."), out evaluateTime);

        private static List<QueueConnection> QueueConnections = new List<QueueConnection>();

        private class QueueConnection
        {
            public string QueueName { get; set; }
            public MQQueue QueueGET { get; set; }
            public MQQueue QueuePUT { get; set; }
        }

        private static string ReadWriteMessageInQueue(string queueName, string message, out long evaluateTime)
        {
            var stopwatch = Stopwatch.StartNew();

            var result = ReadWriteMessageInQueue(queueName, message);

            stopwatch.Stop();
            evaluateTime = stopwatch.ElapsedMilliseconds;

            return result;
        }

        private static string ReadWriteMessageInQueue(string queueName, string message = null)
        {
            try
            {
                var mqmessage = new MQMessage
                {
                    CharacterSet = 1208, // UTF-8
                    Format = MQC.MQFMT_STRING
                };

                if (message == null)
                {
                    GetOrCreateQueue(queueName, true).Get(mqmessage, new MQGetMessageOptions());

                    return mqmessage.ReadLine();
                }
                else
                {
                    mqmessage.WriteString(message);

                    GetOrCreateQueue(queueName, false).Put(mqmessage, new MQPutMessageOptions());

                    return null;
                }
            }
            catch (Exception ex)
            {
                if (ex is MQException mqexception && mqexception.ReasonCode == MQ_EXCEPTION_EMPTY_QUEUE)
                {
                    return null;
                }

                throw ex;
            }
        }

        private static MQQueue GetOrCreateQueue(string queueName, bool isGet)
        {
            var maybeQueueConnection = QueueConnections.FirstOrDefault(qc => qc.QueueName == queueName);

            if (maybeQueueConnection == null)
            {
                maybeQueueConnection = new QueueConnection() { QueueName = queueName };
                QueueConnections.Add(maybeQueueConnection);
            }

            if (isGet)
                return (maybeQueueConnection.QueueGET = maybeQueueConnection.QueueGET ?? GetConnection().AccessQueue(queueName, QUEUE_GET_OPEN_OPTIONS));
            else
                return (maybeQueueConnection.QueuePUT = maybeQueueConnection.QueuePUT ?? GetConnection().AccessQueue(queueName, QUEUE_PUT_OPEN_OPTIONS));
        }

        #endregion


        #region Subscription

        public static List<string> CurrentSubscriptions => Subscriptions.Select(s => s.Name).ToList();
        private static List<Subscription> Subscriptions = new List<Subscription>();

        private class Subscription
        {
            public Thread Thread { get; set; }
            public string Name { get; set; }
        }

        public static void Subscribe(string queueName)
        {
            var sub = new Subscription { Name = queueName };
            sub.Thread = new Thread(() =>
            {
                var localMqQueue = GetConnectionNew(CurrentSettings).AccessQueue(sub.Name, MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_FAIL_IF_QUIESCING);

                while (localMqQueue.IsOpen)
                {
                    try
                    {
                        var messageForGet = new MQMessage();
                        var gmo = new MQGetMessageOptions()
                        {
                            Options = MQC.MQGMO_WAIT | MQC.MQGMO_FAIL_IF_QUIESCING,
                            WaitInterval = 20000, // wait 20 seconds
                        };

                        localMqQueue.Get(messageForGet, gmo);

                        OnSubscriptionEvent?.Invoke(new SubscriptionEventArgs()
                        {
                            Type = SubscriptionEventType.NewMessage,
                            Queue = sub.Name,
                            Message = messageForGet.ReadLine(),
                        });
                    }
                    catch (Exception ex)
                    {
                        if (ex is MQException mqexception && mqexception.ReasonCode == MQ_EXCEPTION_EMPTY_QUEUE)
                        {
                            OnSubscriptionEvent?.Invoke(new SubscriptionEventArgs()
                            {
                                Type = SubscriptionEventType.NoNewMessages,
                                Queue = sub.Name,
                            });

                            continue;
                        }
                        else
                        {
                            OnSubscriptionEvent?.Invoke(new SubscriptionEventArgs()
                            {
                                Type = SubscriptionEventType.Exception,
                                Queue = sub.Name,
                                Exception = ex,
                            });

                            Unsubscribe(sub.Name, false);

                            break;
                        }
                    }
                }
            });
            Subscriptions.Add(sub);
            sub.Thread.Start();
        }

        public static void Unsubscribe(string queueName) => Unsubscribe(queueName, true);
        private static void Unsubscribe(string queueName, bool abortThread)
        {
            var subscription = Subscriptions.FirstOrDefault(x => x.Name == queueName);
            if (subscription != null)
            {
                if (abortThread)
                {
                    subscription.Thread.Abort();
                }

                Subscriptions.Remove(subscription);
                GC.Collect();
            }
        }

        #endregion


        #region Set or Update Connection

        public static bool IsConnected
        {
            get
            {
                var connection = GetConnection();
                return connection == null ? false : connection.IsConnected;
            }
        }
        public static void UpdateConnection(Settings settings) => GetConnection(settings);

        private static MQQueueManager MqQueueManager;
        private static MQQueueManager GetConnection(Settings settings = null)
        {
            if (MqQueueManager == null || settings != null)
            {
                QueueConnections.Clear();
                CurrentSettings = settings ?? CurrentSettings;
                MqQueueManager = new MQQueueManager(CurrentSettings.QueueManagerName, CreateConnectionHashtableProperties(CurrentSettings));
            }

            try
            {
                if (MqQueueManager.IsConnected == false)
                    MqQueueManager.Connect(CurrentSettings.QueueManagerName);
            }
            catch (Exception ex)
            {
                OnSubscriptionEvent?.Invoke(new SubscriptionEventArgs() { Exception = ex, Type = SubscriptionEventType.Exception });
            }
            var ttt = MqQueueManager?.IsConnected;
            return MqQueueManager;
        }

        private static MQQueueManager GetConnectionNew(Settings settings)
        {
            if (settings == null)
                return null;

            try
            {
                var mqQueueManager = new MQQueueManager(settings.QueueManagerName, CreateConnectionHashtableProperties(settings));

                if (mqQueueManager.IsConnected == false)
                    mqQueueManager.Connect(settings.QueueManagerName);

                return mqQueueManager;
            }
            catch (Exception ex)
            {
                OnSubscriptionEvent?.Invoke(new SubscriptionEventArgs() { Exception = ex, Type = SubscriptionEventType.Exception });
            }

            return null;
        }

        private static Hashtable CreateConnectionHashtableProperties(Settings settings)
        {
            var hashtable = new Hashtable
            {
                { MQC.CHANNEL_PROPERTY, settings.Channel },
                { MQC.HOST_NAME_PROPERTY, settings.Host },
                { MQC.PORT_PROPERTY, settings.Port},
            };

            if (string.IsNullOrEmpty(settings.UserId) || string.IsNullOrEmpty(settings.Password))
                return hashtable;

            hashtable.Add(MQC.USER_ID_PROPERTY, settings.UserId);
            hashtable.Add(MQC.PASSWORD_PROPERTY, settings.Password);

            return hashtable;
        }

        #endregion


        #region Event part

        public delegate void SubscriptionEventHandler(SubscriptionEventArgs result);

        public static event SubscriptionEventHandler OnSubscriptionEvent;

        public class SubscriptionEventArgs : EventArgs
        {
            public SubscriptionEventArgs() { }
            public SubscriptionEventArgs(Exception exception)
            {
                Exception = exception;
                Type = SubscriptionEventType.Exception;
            }

            public SubscriptionEventArgs(string message, string messageId, string queue)
            {
                Message = message;
                MessageId = messageId;
                Queue = queue;
                Type = SubscriptionEventType.NewMessage;
            }

            public string Message { get; set; }
            public string MessageId { get; set; }
            public string Queue { get; set; }
            public Exception Exception { get; set; }
            public SubscriptionEventType Type { get; set; }
        }

        public enum SubscriptionEventType { NewMessage, Exception, MqManagerAlreadyConnected, NoNewMessages, Other }

        #endregion
    }
}