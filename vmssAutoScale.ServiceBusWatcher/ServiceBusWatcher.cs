using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vmssAutoScale.Interfaces;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ApplicationInsights;
using System.Threading;

namespace vmssAutoScale.ServiceBusWatcher
{
    public class ServiceBusWatcher : ILoadWatcher
    {

        public string connectionString = "";//"Endpoint=sb://traxservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=z0N5cbbH3+Vc4+bcasDHrJA/Mt9crIs2oNFRdrU+DhQ=";
        //"Endpoint=sb://servicebuszivtest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=4Srucd8nEHCC9tFKwpZrfq1cWhoSOUpsJZam2aMbYgQ=";
        public Microsoft.ServiceBus.NamespaceManager namespaceManager;
        //private Microsoft.Azure.NotificationHubs.NamespaceManager notificationHubNamespaceManager;

        private const int SERVER_UP = 10;
        private const int SERVER_DOWN = 7;
        
        private const string AllEntities = "Entities";
        private const string QueueEntities = "Queues";
        private const string TopicEntities = "Topics";
        private const string SubscriptionEntities = "Subscriptions";
        private const string PartitionEntities = "Partitions";
        private const string ConsumerGroupEntities = "Consumer Groups";
        private const string FilteredQueueEntities = "Queues (Filtered)";
        private const string FilteredTopicEntities = "Topics (Filtered)";
        private const string FilteredSubscriptionEntities = "Subscriptions (Filtered)";

        /*private string _ServiceBusConnectionString;*/
        //private int _ScaleUpBy;
        //private int _ScaleDownBy;
        private string _Topic_A_Name;
        private string _Topic_B_Name;
        private string _Subscription_A_Name;
        private string _Subscription_B_Name;
        private int _ServiceBusMessage_Q_Count_UP;
        private int _ServiceBusMessage_Q_Time_UP;
        private int _ServiceBusMessage_Q_Count_DOWN;
        private int _ServiceBusMessage_Q_Time_Down;

        private ServiceBusHelper serviceBusHelper;
        public static long lngIndex = 0;       

        private TelemetryClient tc = new TelemetryClient();

        public enum EncodingType
        {
            // ReSharper disable once InconsistentNaming
            ASCII,
            // ReSharper disable once InconsistentNaming
            UTF7,
            // ReSharper disable once InconsistentNaming
            UTF8,
            // ReSharper disable once InconsistentNaming
            UTF32,
            Unicode
        }
        public enum EntityType
        {
            All,
            Queue,
            Topic,
            Subscription,
            Rule,
            Relay,
            NotificationHub,
            EventHub,
            ConsumerGroup
        }

        int iGlobalVMRes = 0;

        public double GetCurrentLoad()
        {
            Console.WriteLine("\n");
            Console.WriteLine("Service Bus Monitor Starting......");            

            try
            {                
                ServiceBusWatcher w = new ServiceBusWatcher();
                ThreadMonitorServiceBus();
                
                return iGlobalVMRes;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public void ThreadMonitorServiceBus(/*object data*/)
        {
            tc.InstrumentationKey = "448255fe-beb0-44d9-a019-7a7353a3f39f";// "7972aa41-5375-4134-8a9a-4ddf53a78f80";// Ap6R7CCAr9qz7nQP6G0DqWUclkDMGI6oA/m+eSKTJqw=";

            tc.Context.User.Id = Environment.UserName;
            tc.Context.Session.Id = Guid.NewGuid().ToString();
            tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

            tc.TrackEvent("ServiceBusWatcher");

            namespaceManager = Microsoft.ServiceBus.NamespaceManager.CreateFromConnectionString(connectionString);

            serviceBusHelper = new ServiceBusHelper();
            //serviceBusHelper.OnCreate += serviceBusHelper_OnCreate;
            //serviceBusHelper.OnDelete += serviceBusHelper_OnDelete;

            var serviceBusNamespace = serviceBusHelper.GetServiceBusNamespace("Manual", connectionString);
            bool bFlag = serviceBusHelper.Connect(serviceBusNamespace);

            tc.TrackEvent("GetEntities");
            iGlobalVMRes = GetEntities();

            Console.WriteLine("*******************");
            Console.WriteLine("* Report Complete *");
            Console.WriteLine("*******************");
        }
        
        private int GetEntities()
        {            
            try
            {
                if (serviceBusHelper != null)
                {
                    try
                    {
                        var topics = serviceBusHelper.NamespaceManager.GetTopics(FilterExpressionHelper.TopicFilterExpression);
                        string strText = string.IsNullOrWhiteSpace(FilterExpressionHelper.TopicFilterExpression) ? TopicEntities : FilteredTopicEntities;

                        if (topics != null)
                        {                            
                            foreach (var topic in topics)
                            {
                                if (string.IsNullOrWhiteSpace(topic.Path))
                                {
                                    continue;
                                }
                                string strTopicPath = topic.Path;
                                TopicDescription TopicDesc = topic;
                                
                                try
                                {
                                    var subscriptions = serviceBusHelper.GetSubscriptions(topic, FilterExpressionHelper.SubscriptionFilterExpression);
                                    var subscriptionDescriptions = subscriptions as IList<SubscriptionDescription> ?? subscriptions.ToList();
                                    if ((subscriptions != null && subscriptionDescriptions.Any()) || !string.IsNullOrWhiteSpace(FilterExpressionHelper.SubscriptionFilterExpression))
                                    {
                                        string strSubscriptionText = string.IsNullOrWhiteSpace(FilterExpressionHelper.SubscriptionFilterExpression) ? SubscriptionEntities : FilteredSubscriptionEntities;

                                        foreach (var subscription in subscriptionDescriptions)
                                        {
                                            string strSubscriptionName = subscription.Name;

                                            if (strSubscriptionName.Equals(_Subscription_A_Name) || strSubscriptionName.Equals(_Subscription_B_Name)) 
                                            {
                                                GetQMessageCount(topic, subscription);
                                            }                                            
                                        }

                                        int iVM = CalculteVMSSUpDownStay();
                                        return iVM;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return 0;                                    
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
            return 0;
        }

        public void GetQMessageCount(TopicDescription TopicDesc, SubscriptionDescription SubscriptionDesc)
        {
            SubscriptionWrapper SW = new SubscriptionWrapper(SubscriptionDesc, TopicDesc);
            long lngCount = namespaceManager.GetSubscription(TopicDesc.Path, SubscriptionDesc.Name).MessageCount;                  

            string strlabelMessageCount = lngCount.ToString();

            MessageCountDetails MsgCD = namespaceManager.GetSubscription(TopicDesc.Path, SubscriptionDesc.Name).MessageCountDetails;

            string strLockDuration   = SW.SubscriptionDescription.LockDuration.ToString();
            string strAvilableStatus = SW.SubscriptionDescription.AvailabilityStatus.ToString();
            string strAccessedAt     = SW.SubscriptionDescription.AccessedAt.ToString();

            string strlabelTransferMessageCount = MsgCD.TransferMessageCount.ToString();
            string strlabelActiveMessageCount = MsgCD.ActiveMessageCount.ToString();
            string strlabelScheduledMessageCount = MsgCD.ScheduledMessageCount.ToString();
            string strlabelDeadLetterMessageCount = MsgCD.DeadLetterMessageCount.ToString();

            string strlabelMaxDeliveryCount = namespaceManager.GetSubscription(TopicDesc.Path, SubscriptionDesc.Name).MaxDeliveryCount.ToString();

            String strStatus = namespaceManager.GetSubscription(TopicDesc.Path, SubscriptionDesc.Name).Status.ToString();
            string strlabelStatus = strStatus;
            
            tc.TrackEvent("GetQMessageCount");
            tc.TrackMetric("Topic: " + SW.TopicDescription.Path, lngCount);
            tc.TrackMetric(SW.SubscriptionDescription.Name, lngCount);
            //tc.TrackMetric(SW.SubscriptionDescription.Name + "Avilable Status: ", strAvilableStatus);            
            tc.TrackMetric("TransferMessageCount", MsgCD.TransferMessageCount);
            tc.TrackMetric("ActiveMessageCount", MsgCD.ActiveMessageCount);
            tc.TrackMetric("ScheduledMessageCount", MsgCD.ScheduledMessageCount);
            tc.TrackMetric("DeadLetterMessageCount", MsgCD.DeadLetterMessageCount);

            lngIndex++;
            Console.WriteLine(lngIndex.ToString() + ".\n");            
            Console.WriteLine("Topic: " + SW.TopicDescription.Path);
            Console.WriteLine("Subscription: " + SW.SubscriptionDescription.Name);
            Console.WriteLine("Messages Count: " + lngCount);
            Console.WriteLine("Status: " + strStatus);
            Console.WriteLine("TransferMessageCount:" + MsgCD.TransferMessageCount);
            Console.WriteLine("ActiveMessageCount:" + MsgCD.ActiveMessageCount);
            Console.WriteLine("ScheduledMessageCount:" + MsgCD.ScheduledMessageCount);
            Console.WriteLine("DeadLetterMessageCount:" + MsgCD.DeadLetterMessageCount);
            Console.WriteLine("Lock Duration:" + strLockDuration);
            Console.WriteLine("Avilable Status:" + strAvilableStatus);
            Console.WriteLine("Accessed At:" + strAccessedAt);

            Console.WriteLine("\n");

            GetMessages(true, true, (int)lngCount,SW);            
        }

        public List<BrokeredMessage> TotalMsgBrokerList = new List<BrokeredMessage>();

        private void GetMessages(bool peek, bool all, int count, SubscriptionWrapper SW)
        {
            try
            {
                var brokeredMessages = new List<BrokeredMessage>();
                if (peek)
                {
                    var subscriptionClient = serviceBusHelper.MessagingFactory.CreateSubscriptionClient(SW.SubscriptionDescription.TopicPath,
                                                                                                        SW.SubscriptionDescription.Name,
                                                                                                        ReceiveMode.PeekLock);
                    var totalRetrieved = 0;
                    while (totalRetrieved < count)
                    {
                        var messageEnumerable = subscriptionClient.PeekBatch(count);
                        if (messageEnumerable == null)
                        {
                            break;
                        }
                        
                        var messageArray = messageEnumerable as BrokeredMessage[] ?? messageEnumerable.ToArray();
                        var partialList = new List<BrokeredMessage>(messageArray);
                        
                        brokeredMessages.AddRange(partialList);
                        totalRetrieved += partialList.Count;
                        if (partialList.Count == 0)
                        {
                            break;
                        }
                        else
                        {
                            for (int iIndex = 0; iIndex < partialList.Count; iIndex++)
                            {
                                BrokeredMessage bMsg = partialList[iIndex];
                                TotalMsgBrokerList.Add(bMsg);
                            }                                                           
                        }
                    }                   
                }
            }
            catch (TimeoutException)
            {
                int x = 0;
            }            
        }

        public int CalculteVMSSUpDownStay()
        {
            int TimeLimitUp = 5;
            int MsgCountUp = 10;
            int VMMCountUp = 2;

            int TimeLimitDown = 15;
            int MsgCountDown = 8;
            int VMMCountXown = 1;

            int iIndexMsgUp = 0;
            int iIndexMsgDown = 0;

            for (int iIndex = 0; iIndex < TotalMsgBrokerList.Count; iIndex++)
            {
                BrokeredMessage bMsg = TotalMsgBrokerList[iIndex];                

                string strMsgStringTime = bMsg.EnqueuedTimeUtc.ToString();
                Console.WriteLine("Message Time Created: " + strMsgStringTime);

                DateTime CurTime = DateTime.Now;
                string strCurrentTime = CurTime.ToString();
                Console.WriteLine("Current Time: " + strCurrentTime);

                TimeSpan t = CurTime - bMsg.EnqueuedTimeUtc;
                string strTimeSpan = t.TotalMinutes.ToString();

                Console.WriteLine("Time Span Minutes: " + strTimeSpan);

                if(t.TotalMinutes > TimeLimitUp)
                {
                    iIndexMsgUp++;
                }

                if (t.TotalMinutes < TimeLimitDown)
                {
                    iIndexMsgDown++;
                }
            }

            if(iIndexMsgUp > TimeLimitUp)
            {
                return SERVER_UP;// 10;
            }
            else if(iIndexMsgDown < 8)
            {
                return SERVER_DOWN;// 7;
            }

            return 0;
        }

        /*
        public void GetQMessageCount()
        {
            long lngCount = namespaceManager.GetSubscription("TestTopic", "AllMessages").MessageCount;

            string strlabelMessageCount = lngCount.ToString();

            MessageCountDetails MsgCD = namespaceManager.GetSubscription("TestTopic", "AllMessages").MessageCountDetails;

            string strlabelTransferMessageCount = MsgCD.TransferMessageCount.ToString();
            string strlabelActiveMessageCount = MsgCD.ActiveMessageCount.ToString();
            string strlabelScheduledMessageCount = MsgCD.ScheduledMessageCount.ToString();
            string strlabelDeadLetterMessageCount = MsgCD.DeadLetterMessageCount.ToString();

            string strlabelMaxDeliveryCount = namespaceManager.GetSubscription("TestTopic", "AllMessages").MaxDeliveryCount.ToString();

            String strStatus = namespaceManager.GetSubscription("TestTopic", "AllMessages").Status.ToString();
            string strlabelStatus = strStatus;
            
        }*/

        

        public void Set_Topic_A_Name(string strTopic_A_Name) { _Topic_A_Name = strTopic_A_Name; }

        public void Set_Topic_B_Name(string strTopic_B_Name) { _Topic_B_Name = strTopic_B_Name; }

        public void Set_Subscription_A_Name(string strSubscription_A_Name) { _Subscription_A_Name = strSubscription_A_Name; }

        public void Set_Subscription_B_Name(string strSubscription_B_Name) { _Subscription_B_Name = strSubscription_B_Name; }

        public void Set_ServiceBusMessage_Q_Count_UP(int iServiceBusMessage_Q_Count_UP) { _ServiceBusMessage_Q_Count_UP = iServiceBusMessage_Q_Count_UP; }

        public void Set_ServiceBusMessage_Q_Count_DOWN(int iServiceBusMessage_Q_Count_DOWN) { _ServiceBusMessage_Q_Count_DOWN = iServiceBusMessage_Q_Count_DOWN; }

        public void Set_ServiceBusMessage_Q_Time_UP(int iServiceBusMessage_Q_Time_UP) { _ServiceBusMessage_Q_Time_UP = iServiceBusMessage_Q_Time_UP; }

        public void Set_ServiceBusMessage_Q_Time_Down(int iServiceBusMessage_Q_Time_Down) { _ServiceBusMessage_Q_Time_Down = iServiceBusMessage_Q_Time_Down; }
                
        /*
        void serviceBusHelper_OnCreate(ServiceBusHelperEventArgs args)
        {
        }

        void serviceBusHelper_OnDelete(ServiceBusHelperEventArgs args)
        {
        }*/


    }
}
