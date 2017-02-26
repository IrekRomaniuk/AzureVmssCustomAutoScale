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

        public string connectionString = "Endpoint=sb://servicebuszivtest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=4Srucd8nEHCC9tFKwpZrfq1cWhoSOUpsJZam2aMbYgQ=";
        public Microsoft.ServiceBus.NamespaceManager namespaceManager;
        private Microsoft.Azure.NotificationHubs.NamespaceManager notificationHubNamespaceManager;
        

        //***************************
        // Formats
        //***************************
        private const string DateFormat = "<{0,2:00}:{1,2:00}:{2,2:00}> {3}";
        private const string ExceptionFormat = "Exception: {0}";
        private const string InnerExceptionFormat = "InnerException: {0}";
        private const string LogFileNameFormat = "ServiceBusExplorer {0}.txt";
        private const string EntityFileNameFormat = "{0} {1} {2}.xml";
        private const string EntitiesFileNameFormat = "{0} {1}.xml";
        private const string UrlSegmentFormat = "{0}/{1}";
        //private const string FaultNode = "Fault";
        private const string NameMessageCountFormat = "{0} ({1}, {2})";
        private const string PartitionFormat = "{0,2:00}";

        //***************************
        // Messages
        //***************************
        private const string ServiceBusNamespacesNotConfigured = "Service bus accounts have not been properly configured in the configuration file.";
        private const string ServiceBusNamespaceIsNullOrEmpty = "The connection string for service bus entry {0} is null or empty.";
        private const string ServiceBusNamespaceIsWrong = "The connection string for service bus namespace {0} is in the wrong format.";
        private const string ServiceBusNamespaceNamespaceAndUriAreNullOrEmpty = "Both the uri and namespace for the service bus entry {0} is null or empty.";
        private const string ServiceBusNamespaceIssuerNameIsNullOrEmpty = "The issuer name for the service bus namespace {0} is null or empty.";
        private const string ServiceBusNamespaceIssuerSecretIsNullOrEmpty = "The issuer secret for the service bus namespace {0} is null or empty.";
        private const string ServiceBusNamespaceEndpointIsNullOrEmpty = "The endpoint for the service bus namespace {0} is null or empty.";
        private const string ServiceBusNamespaceStsEndpointIsNullOrEmpty = "The sts endpoint for the service bus namespace {0} is null or empty.";
        private const string ServiceBusNamespaceRuntimePortIsNullOrEmpty = "The runtime port for the service bus namespace {0} is null or empty.";
        private const string ServiceBusNamespaceManagementPortIsNullOrEmpty = "The management port for the service bus namespace {0} is null or empty.";
        private const string ServiceBusNamespaceEndpointUriIsInvalid = "The endpoint uri for the service bus namespace {0} is invalid.";
        private const string ServiceBusNamespaceSharedAccessKeyNameIsInvalid = "The SharedAccessKeyName for the service bus namespace {0} is invalid.";
        private const string ServiceBusNamespaceSharedAccessKeyIsInvalid = "The SharedAccessKey for the service bus namespace {0} is invalid.";
        private const string QueueRetrievedFormat = "The queue {0} has been successfully retrieved.";
        private const string TopicRetrievedFormat = "The topic {0} has been successfully retrieved.";
        private const string RelayRetrievedFormat = "The relay {0} has been successfully retrieved.";
        private const string SubscriptionRetrievedFormat = "The subscription {0} for the {1} topic has been successfully retrieved.";
        private const string RuleRetrievedFormat = "The rule {0} for the {1} subscription of the {2} topic has been successfully retrieved.";
        private const string EventHubRetrievedFormat = "The event hub {0} has been successfully retrieved.";
        private const string PartitionsRetrievedFormat = "{0} partitions for the event hub {1} have been successfully retrieved.";
        private const string ConsumerGroupsRetrievedFormat = "{0} consumer groups for the event hub {1} have been successfully retrieved.";
        private const string PartitionRetrievedFormat = "The partition {0} of the event hub {1} has been successfully retrieved.";
        private const string ConsumerGroupRetrievedFormat = "The consumer group {0} of the event hub {1} has been successfully retrieved.";
        private const string NotificationHubRetrievedFormat = "The notification hub {0} has been successfully retrieved.";
        //private const string SyndicateItemFormat = "The atom feed item {0} has been successfully retrieved.";
        //private const string LinkUriFormat = "The link uri {0} has been successfully retrieved.";
        private const string TestQueueFormat = "Test Queue: {0}";
        private const string TestTopicFormat = "Test Topic: {0}";
        private const string TestSubscriptionFormat = "Test Subscription: {0}";
        private const string CreateQueue = "Create Queue";
        private const string CreateTopic = "Create Topic";
        private const string CreateRelay = "Create Relay";
        private const string CreateSubscription = "Create Subscription";
        private const string CreateEventHub = "Create Event Hub";
        private const string CreateConsumerGroup = "Create Consumer Group";
        private const string CreateNotificationHub = "Create Notification Hub";
        private const string AddRule = "Add Rule";
        private const string ViewQueueFormat = "View Queue: {0}";
        private const string ViewTopicFormat = "View Topic: {0}";
        private const string ViewSubscriptionFormat = "View Subscription: {0}";
        private const string ViewRelayFormat = "View Relay: {0}";
        private const string ViewRuleFormat = "View Rule: {0}";
        private const string ViewEventHubFormat = "View Event Hub: {0}";
        private const string ViewPartitionFormat = "View Partition: {0}";
        private const string ViewConsumerGroupFormat = "View Consumer Group: {0}";
        private const string ViewNotificationHubFormat = "View Notification Hub: {0}";
        private const string TestRelayFormat = "Test Relay: {0}";
        private const string DeleteAllEntities = "All the entities will be permanently deleted.";
        private const string DeleteAllQueues = "All the queues will be permanently deleted.";
        private const string DeleteAllQueuesInPath = "All the queues in [{0}] will be permanently deleted.";
        private const string DeleteAllTopics = "All the topics will be permanently deleted.";
        private const string DeleteAllTopicsInPath = "All the topics in [{0}] will be permanently deleted.";
        private const string DeleteAllRelays = "All the relays will be permanently deleted.";
        private const string DeleteAllRelaysInPath = "All the relays in [{0}] will be permanently deleted.";
        private const string DeleteAllSubscriptions = "All the subscriptions will be permanently deleted.";
        private const string DeleteAllRules = "All the rules will be permanently deleted.";
        private const string DeleteAllEventHubs = "All the event hubs will be permanently deleted.";
        private const string DeleteAllConsumerGroups = "All the consumer groups of the event hub {0} will be permanently deleted with the exception of the $Default.";
        private const string DeleteAllNotificationHubs = "All the notification hubs will be permanently deleted.";
        private const string EntitiesExported = "Selected entities have been exported to {0}.";
        private const string EntitiesImported = "Entities have been imported from {0}.";
        private const string EnableQueue = "Enable Queue";
        private const string DisableQueue = "Disable Queue";
        private const string EnableTopic = "Enable Topic";
        private const string DisableTopic = "Disable Topic";
        private const string EnableSubscription = "Enable Subscription";
        private const string DisableSubscription = "Disable Subscription";
        private const string EnableEventHub = "Enable Event Hub";
        private const string DisableEventHub = "Disable Event Hub";
        private const string SubscriptionIdCannotBeNull = "In order to use metrics, you need to define the Microsoft Azure Subscription Id in the configuration file or Options form.";
        private const string ManagementCertificateThumbprintCannotBeNull = "In order to use metrics, you need to define in the configuration file or Options form the thumbprint of a valid management certificate for your Microsoft Azure subscription.";
        private const string NoNamespaceWithKeyMessageFormat = "No namespace with key equal to [{0}] exists in the serviceBusNamespaces section of the configuration file.";

        //***************************
        // Constants
        //***************************
        private const string ServiceBusNamespaces = "serviceBusNamespaces";
        private const string BrokeredMessageInspectors = "brokeredMessageInspectors";
        private const string EventDataInspectors = "eventDataInspectors";
        private const string BrokeredMessageGenerators = "brokeredMessageGenerators";
        private const string EventDataGenerators = "eventDataGenerators";
        //private const string UrlEntity = "Url";
        private const string AllEntities = "Entities";
        private const string QueueEntities = "Queues";
        private const string TopicEntities = "Topics";
        private const string SubscriptionEntities = "Subscriptions";
        private const string PartitionEntities = "Partitions";
        private const string ConsumerGroupEntities = "Consumer Groups";
        private const string FilteredQueueEntities = "Queues (Filtered)";
        private const string FilteredTopicEntities = "Topics (Filtered)";
        private const string FilteredSubscriptionEntities = "Subscriptions (Filtered)";
        private const string RuleEntities = "Rules";
        private const string RelayEntities = "Relays";
        private const string EventHubEntities = "Event Hubs";
        private const string NotificationHubEntities = "Notification Hubs";
        private const string QueueEntity = "Queue";
        private const string TopicEntity = "Topic";
        private const string SubscriptionEntity = "Subscription";
        private const string RuleEntity = "Rule";
        private const string RelayEntity = "Relay";
        private const string EventHubEntity = "Event Hub";
        private const string ConsumerGroupEntity = "Consumer Group";
        private const string NotificationHubEntity = "Notification Hub";
        private const string Entity = "Entity";
        private const string SaveAsTitle = "Save Log As";
        private const string SaveEntityAsTitle = "Save File As";
        private const string OpenEntityAsTitle = "Open File";
        private const string SaveAsExtension = "txt";
        private const string XmlExtension = "xml";
        private const string SaveAsFilter = "Text Documents|*.txt";
        private const string XmlFilter = "XML Files|*.xml";
        private const string DefaultMessageText = "Hi mate, how are you?";
        private const string DefaultLabel = "Service Bus Explorer";
        private const string ImportToolStripMenuItemName = "importEntityMenuItem2";
        private const string ImportToolStripMenuItemText = "Import Entities";
        private const string ImportToolStripMenuItemToolTipText = "Import entity definition from file.";
        private const string EventClick = "EventClick";
        private const string EventsProperty = "Events";
        private const string ChangeStatusQueueMenuItem = "changeStatusQueueMenuItem";
        private const string ChangeStatusTopicMenuItem = "changeStatusTopicMenuItem";
        private const string ChangeStatusSubscriptionMenuItem = "changeStatusSubscriptionMenuItem";
        private const string ChangeStatusEventHubMenuItem = "changeStatusEventHubMenuItem";
        private const string MetricsHeader = "Namespace Metrics";
        private const string DefaultConsumerGroupName = "$Default";

        //***************************
        // Parameters
        //***************************
        private const string ConnectionStringUri = "uri";
        private const string ConnectionStringNameSpace = "namespace";
        private const string ConnectionStringServicePath = "servicepath";
        private const string ConnectionStringIssuerName = "issuername";
        private const string ConnectionStringIssuerSecret = "issuersecret";
        private const string ConnectionStringOwner = "owner";
        private const string ConnectionStringEndpoint = "endpoint";
        private const string ConnectionStringSharedAccessKeyName = "sharedaccesskeyname";
        private const string ConnectionStringSharedAccessKey = "sharedaccesskey";
        private const string ConnectionStringStsEndpoint = "stsendpoint";
        private const string ConnectionStringRuntimePort = "runtimeport";
        private const string ConnectionStringManagementPort = "managementport";
        private const string ConnectionStringWindowsUsername = "windowsusername";
        private const string ConnectionStringWindowsDomain = "windowsdomain";
        private const string ConnectionStringWindowsPassword = "windowspassword";
        private const string ConnectionStringSharedSecretIssuer = "sharedsecretissuer";
        private const string ConnectionStringSharedSecretValue = "sharedsecretvalue";
        private const string ConnectionStringTransportType = "transporttype";

       

        private ServiceBusHelper serviceBusHelper;
        public static long lngIndex = 0;

        //private SubscriptionWrapper subscriptionWrapper;
        //TreeNode rootNode;
        //TreeNode subscriptionRootNode;

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

        public double GetCurrentLoad()
        {
            Console.WriteLine("\n");
            Console.WriteLine("Service Bus Monitor Starting......");
            Console.WriteLine("\n");            

            try
            {                
                ServiceBusWatcher w = new ServiceBusWatcher();
                Thread newThread = new Thread(w.ThreadMonitorServiceBus);
                newThread.Start("The answer.");                   
                

                System.Threading.Thread.Sleep(10000000);

                notificationHubNamespaceManager = Microsoft.Azure.NotificationHubs.NamespaceManager.CreateFromConnectionString(connectionString);
                if (notificationHubNamespaceManager != null)
                {
                    GetQMessageCount();
                }

                return 5;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public void ThreadMonitorServiceBus(object data)
        {
            tc.InstrumentationKey = "7972aa41-5375-4134-8a9a-4ddf53a78f80";// "a22ec41f -0ffa-4bd8-a745-d982cf9c1268";

            tc.Context.User.Id = Environment.UserName;
            tc.Context.Session.Id = Guid.NewGuid().ToString();
            tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

            tc.TrackEvent("ServiceBusWatcher");

            namespaceManager = Microsoft.ServiceBus.NamespaceManager.CreateFromConnectionString(connectionString);

            serviceBusHelper = new ServiceBusHelper();
            serviceBusHelper.OnCreate += serviceBusHelper_OnCreate;
            serviceBusHelper.OnDelete += serviceBusHelper_OnDelete;

            var serviceBusNamespace = GetServiceBusNamespace("Manual", connectionString);
            bool bFlag = serviceBusHelper.Connect(serviceBusNamespace);

            for (int iIndex = 0; iIndex < 6000; iIndex++)
            {
                tc.TrackEvent("GetEntities: " + (iIndex + 1));
                GetEntities(EntityType.All);

                System.Threading.Thread.Sleep(500);
            }

            Console.WriteLine("*******************");
            Console.WriteLine("* Report Complete *");
            Console.WriteLine("*******************");
        }

        private ServiceBusNamespace GetServiceBusNamespace(string key, string connectionString)
        {

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceIsNullOrEmpty, key));
                return null;
            }

            var toLower = connectionString.ToLower();
            var parameters = connectionString.Split(';').ToDictionary(s => s.Substring(0, s.IndexOf('=')).ToLower(), s => s.Substring(s.IndexOf('=') + 1));

            if (toLower.Contains(ConnectionStringEndpoint) &&
                toLower.Contains(ConnectionStringSharedAccessKeyName) &&
                toLower.Contains(ConnectionStringSharedAccessKey))
            {
                if (parameters.Count < 3)
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceIsWrong, key));
                    return null;
                }
                var endpoint = parameters.ContainsKey(ConnectionStringEndpoint) ?
                               parameters[ConnectionStringEndpoint] :
                               null;

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceEndpointIsNullOrEmpty, key));
                    return null;
                }

                var stsEndpoint = parameters.ContainsKey(ConnectionStringStsEndpoint) ?
                                  parameters[ConnectionStringStsEndpoint] :
                                  null;

                Uri uri;
                try
                {
                    uri = new Uri(endpoint);
                }
                catch (Exception)
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceEndpointUriIsInvalid, key));
                    return null;
                }
                var ns = uri.Host.Split('.')[0];

                if (!parameters.ContainsKey(ConnectionStringSharedAccessKeyName) || string.IsNullOrWhiteSpace(parameters[ConnectionStringSharedAccessKeyName]))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceSharedAccessKeyNameIsInvalid, key));
                }
                var sharedAccessKeyName = parameters[ConnectionStringSharedAccessKeyName];

                if (!parameters.ContainsKey(ConnectionStringSharedAccessKey) || string.IsNullOrWhiteSpace(parameters[ConnectionStringSharedAccessKey]))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceSharedAccessKeyIsInvalid, key));
                }
                var sharedAccessKey = parameters[ConnectionStringSharedAccessKey];


                var settings = new MessagingFactorySettings();
                var transportType = settings.TransportType;

                if (parameters.ContainsKey(ConnectionStringTransportType))
                {
                    Enum.TryParse(parameters[ConnectionStringTransportType], true, out transportType);
                }

                return new ServiceBusNamespace(ServiceBusNamespaceType.Cloud, connectionString, endpoint, ns, null, sharedAccessKeyName, sharedAccessKey, stsEndpoint, transportType, true);
            }

            if (toLower.Contains(ConnectionStringRuntimePort) ||
                toLower.Contains(ConnectionStringManagementPort) ||
                toLower.Contains(ConnectionStringWindowsUsername) ||
                toLower.Contains(ConnectionStringWindowsDomain) ||
                toLower.Contains(ConnectionStringWindowsPassword))
            {
                if (!toLower.Contains(ConnectionStringEndpoint) ||
                    !toLower.Contains(ConnectionStringStsEndpoint) ||
                    !toLower.Contains(ConnectionStringRuntimePort) ||
                    !toLower.Contains(ConnectionStringManagementPort))
                {
                    return null;
                }

                var endpoint = parameters.ContainsKey(ConnectionStringEndpoint) ?
                               parameters[ConnectionStringEndpoint] :
                               null;

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceEndpointIsNullOrEmpty, key));
                    return null;
                }

                Uri uri;
                try
                {
                    uri = new Uri(endpoint);
                }
                catch (Exception)
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceEndpointUriIsInvalid, key));
                    return null;
                }
                var ns = uri.Host.Split('.')[0];

                var stsEndpoint = parameters.ContainsKey(ConnectionStringStsEndpoint) ?
                                  parameters[ConnectionStringStsEndpoint] :
                                  null;

                if (string.IsNullOrWhiteSpace(stsEndpoint))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceStsEndpointIsNullOrEmpty, key));
                    return null;
                }

                var runtimePort = parameters.ContainsKey(ConnectionStringRuntimePort) ?
                                  parameters[ConnectionStringRuntimePort] :
                                  null;

                if (string.IsNullOrWhiteSpace(runtimePort))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceRuntimePortIsNullOrEmpty, key));
                    return null;
                }

                var managementPort = parameters.ContainsKey(ConnectionStringManagementPort) ?
                                     parameters[ConnectionStringManagementPort] :
                                     null;

                if (string.IsNullOrWhiteSpace(managementPort))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceManagementPortIsNullOrEmpty, key));
                    return null;
                }

                var windowsDomain = parameters.ContainsKey(ConnectionStringWindowsDomain) ?
                                    parameters[ConnectionStringWindowsDomain] :
                                    null;

                var windowsUsername = parameters.ContainsKey(ConnectionStringWindowsUsername) ?
                                      parameters[ConnectionStringWindowsUsername] :
                                      null;

                var windowsPassword = parameters.ContainsKey(ConnectionStringWindowsPassword) ?
                                      parameters[ConnectionStringWindowsPassword] :
                                      null;
                var settings = new MessagingFactorySettings();
                var transportType = settings.TransportType;
                if (parameters.ContainsKey(ConnectionStringTransportType))
                {
                    Enum.TryParse(parameters[ConnectionStringTransportType], true, out transportType);
                }
                return new ServiceBusNamespace(connectionString, endpoint, stsEndpoint, runtimePort, managementPort, windowsDomain, windowsUsername, windowsPassword, ns, transportType);
            }

            if (toLower.Contains(ConnectionStringEndpoint) &&
                toLower.Contains(ConnectionStringSharedSecretIssuer) &&
                toLower.Contains(ConnectionStringSharedSecretValue))
            {
                if (parameters.Count < 3)
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceIsWrong, key));
                    return null;
                }

                var endpoint = parameters.ContainsKey(ConnectionStringEndpoint) ?
                               parameters[ConnectionStringEndpoint] :
                               null;

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceEndpointIsNullOrEmpty, key));
                    return null;
                }

                var stsEndpoint = parameters.ContainsKey(ConnectionStringStsEndpoint) ?
                                  parameters[ConnectionStringStsEndpoint] :
                                  null;

                Uri uri;
                try
                {
                    uri = new Uri(endpoint);
                }
                catch (Exception)
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceEndpointUriIsInvalid, key));
                    return null;
                }
                var ns = uri.Host.Split('.')[0];
                var issuerName = parameters.ContainsKey(ConnectionStringSharedSecretIssuer) ?
                                     parameters[ConnectionStringSharedSecretIssuer] :
                                     ConnectionStringOwner;

                if (!parameters.ContainsKey(ConnectionStringSharedSecretValue) ||
                    string.IsNullOrWhiteSpace(parameters[ConnectionStringSharedSecretValue]))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceIssuerSecretIsNullOrEmpty, key));
                    return null;

                }
                var issuerSecret = parameters[ConnectionStringSharedSecretValue];

                var settings = new MessagingFactorySettings();
                var transportType = settings.TransportType;
                if (parameters.ContainsKey(ConnectionStringTransportType))
                {
                    Enum.TryParse(parameters[ConnectionStringTransportType], true, out transportType);
                }

                return new ServiceBusNamespace(ServiceBusNamespaceType.Cloud, connectionString, endpoint, ns, null, issuerName, issuerSecret, stsEndpoint, transportType);
            }
            else
            {
                if (parameters.Count < 4)
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceIsWrong, key));
                    return null;
                }

                var uriString = parameters.ContainsKey(ConnectionStringUri) ?
                                    parameters[ConnectionStringUri] :
                                    null;

                if (string.IsNullOrWhiteSpace(uriString) && !parameters.ContainsKey(ConnectionStringNameSpace))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceNamespaceAndUriAreNullOrEmpty, key));
                    return null;
                }

                var ns = parameters[ConnectionStringNameSpace];

                var servicePath = parameters.ContainsKey(ConnectionStringServicePath) ?
                                      parameters[ConnectionStringServicePath] :
                                      null;

                if (!parameters.ContainsKey(ConnectionStringIssuerName))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceIssuerNameIsNullOrEmpty, key));
                    return null;
                }
                var issuerName = parameters.ContainsKey(ConnectionStringIssuerName) ?
                                     parameters[ConnectionStringIssuerName] :
                                     ConnectionStringOwner;

                if (!parameters.ContainsKey(ConnectionStringIssuerSecret) ||
                    string.IsNullOrWhiteSpace(parameters[ConnectionStringIssuerSecret]))
                {
                    //WriteToLog(string.Format(CultureInfo.CurrentCulture, ServiceBusNamespaceIssuerSecretIsNullOrEmpty, key));
                    return null;

                }
                var issuerSecret = parameters[ConnectionStringIssuerSecret];

                var settings = new MessagingFactorySettings();
                var transportType = settings.TransportType;
                if (parameters.ContainsKey(ConnectionStringTransportType))
                {
                    Enum.TryParse(parameters[ConnectionStringTransportType], true, out transportType);
                }

                return new ServiceBusNamespace(ServiceBusNamespaceType.Custom, connectionString, uriString, ns, servicePath, issuerName, issuerSecret, null, transportType);
            }
        }

        private void GetEntities(EntityType entityType)
        {
            //var updating = false;
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

                                            GetQMessageCount(topic, subscription);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    int x = 1;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        int x = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                int x = 1;
            }
        }

        public void GetQMessageCount(TopicDescription TopicDesc, SubscriptionDescription SubscriptionDesc)
        {
            SubscriptionWrapper SW = new SubscriptionWrapper(SubscriptionDesc, TopicDesc);
            long lngCount = namespaceManager.GetSubscription(TopicDesc.Path, SubscriptionDesc.Name).MessageCount;

            string strlabelMessageCount = lngCount.ToString();

            MessageCountDetails MsgCD = namespaceManager.GetSubscription(TopicDesc.Path, SubscriptionDesc.Name).MessageCountDetails;

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
            Console.WriteLine("\n");



            GetMessages(true, true, (int)lngCount,SW);            
          
        }
             
        

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

                        IBrokeredMessageInspector messageInspector = null;
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
                            for (int iIndex = 0;iIndex < partialList.Count;iIndex++)
                            {                                
                                BrokeredMessage bMsg = partialList[iIndex];
                                int x = 0;
                            }
                        }
                    }
                    //writeToLog(string.Format(MessagesPeekedFromTheSubscription, brokeredMessages.Count, subscriptionWrapper.SubscriptionDescription.Name));
                }
                else
                {
                    MessageReceiver messageReceiver;
                    if (SW.SubscriptionDescription.RequiresSession)
                    {
                        var subscriptionClient = serviceBusHelper.MessagingFactory.CreateSubscriptionClient(SW.SubscriptionDescription.TopicPath,
                                                                                                            SW.SubscriptionDescription.Name,
                                                                                                            ReceiveMode.ReceiveAndDelete);
                        //messageReceiver = subscriptionClient.AcceptMessageSession(TimeSpan.FromSeconds(MainForm.SingletonMainForm.ReceiveTimeout));
                    }
                    else
                    {
                        messageReceiver = serviceBusHelper.MessagingFactory.CreateMessageReceiver(SubscriptionClient.FormatSubscriptionPath(
                                                                                                  SW.SubscriptionDescription.TopicPath,
                                                                                                  SW.SubscriptionDescription.Name),
                                                                                                  ReceiveMode.ReceiveAndDelete);
                    }
                    
                    //writeToLog(string.Format(MessagesReceivedFromTheSubscription, brokeredMessages.Count, subscriptionWrapper.SubscriptionDescription.Name));
                }
                
                if (!peek)
                {
                    /*if (OnRefresh != null)
                    {
                        OnRefresh();
                    }*/
                }
                
            }
            catch (TimeoutException)
            {
                /*writeToLog(string.Format(NoMessageReceivedFromTheSubscription,
                                         MainForm.SingletonMainForm.ReceiveTimeout,
                                         subscriptionWrapper.SubscriptionDescription.Name));*/

                int x = 0;
            }
            
        }


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
            /*
            var topics = serviceBusHelper.NamespaceManager.GetTopics(FilterExpressionHelper.TopicFilterExpression);
            topicListNode.Text = string.IsNullOrWhiteSpace(FilterExpressionHelper.TopicFilterExpression)
                ? TopicEntities
                : FilteredTopicEntities;
            topicListNode.Nodes.Clear();
            if (topics != null)
            {
                foreach (var topic in topics)
                {
                    if (string.IsNullOrWhiteSpace(topic.Path))
                    {
                        continue;
                    }
                    var entityNode = CreateNode(topic.Path, topic, topicListNode, true);


                }*/
        }

        void serviceBusHelper_OnCreate(ServiceBusHelperEventArgs args)
        {
        }

        void serviceBusHelper_OnDelete(ServiceBusHelperEventArgs args)
        {
        }

        
    }
}
