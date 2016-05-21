using System;
using System.Configuration;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Homeworld.Tracker.Web.Domain;
using Homeworld.Tracker.Web.Dtos;
using Homeworld.Tracker.Web.Models;
using Microsoft.Data.Entity;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Homeworld.Tracker.web.Domain
{
    public static class MqttWrapper
    {
        private static MqttClient _client;
        private static IMovementService _movementService;

        public static void Connect()
        {
            _client = new MqttClient("m21.cloudmqtt.com", 10891, false,
                new X509Certificate(), new X509Certificate(), MqttSslProtocols.None);
          
            var code = _client.Connect(Guid.NewGuid().ToString(), "apzvvubw", "bqqhHe9qGf1A");

            var msgId = _client.Subscribe(new[] { "location/+/movement" },
                new[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

            //Wire up events
            _client.MqttMsgSubscribed += ClientOnMqttMsgSubscribed;
            _client.MqttMsgPublishReceived += ClientOnMqttMsgPublishReceived;

            //new up movement service
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer(@"Data Source = homeworld.database.windows.net; Initial Catalog = Tracker; Integrated Security = False; User ID = homeworld; Password = Poppycock@121; Connect Timeout = 60; Encrypt = False; TrustServerCertificate = False; ApplicationIntent = ReadWrite; MultiSubnetFailover = False");
            var context = new TrackerDbContext(optionsBuilder.Options);

            _movementService = new MovementService(context);
        }

        private static void ClientOnMqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Debug.WriteLine("Received = " + Encoding.UTF8.GetString(e.Message) + " on topic " + e.Topic);
            var data = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(e.Message));

            DateTime swipeTimeUTC;
            var result = DateTime.TryParse(data.SwipeTime.ToString(), out swipeTimeUTC);

            swipeTimeUTC = result ? swipeTimeUTC : DateTime.UtcNow;

            var movementData = new MovementDto {DeviceId = data.DeviceId, Uid = data.CardId, SwipeTime = swipeTimeUTC};

            _movementService.Save(movementData);
        }

        private static void ClientOnMqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            Debug.WriteLine("Subscribed for id = " + e.MessageId);
        }

        public static void Disconnect()
        {
            _client.Disconnect();
        }
    }
}
