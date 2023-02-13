using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Controls;
using HslCommunication;
using HslCommunication.MQTT;

namespace MqttExample
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        private MqttClient mqttClient;
        private System.Timers.Timer _timerFirstConnect;
        public bool IsConnected { get; private set; } = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // connect MQTT first when startup app
            _timerFirstConnect = new System.Timers.Timer();
            _timerFirstConnect.Interval = 2000;
            _timerFirstConnect.Elapsed += _timerFirstConnect_Elapsed;
        }

        private async void _timerFirstConnect_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timerFirstConnect.Enabled = false;

            try
            {
                var BrokerAddress = TxtMqttBroker.Text;
                // use a unique id as client id, each time we start the application
                var clientId = Guid.NewGuid().ToString();

                mqttClient = new MqttClient(
                        new MqttConnectionOptions()
                        {
                            ClientId = clientId,
                            IpAddress = BrokerAddress,
                            Port = 1883,
                            KeepAlivePeriod = TimeSpan.FromSeconds(3)
                            //Credentials = new MqttCredential("admin", "admin"), // 设置了用户名和密码
                        }
                    );

                mqttClient.OnNetworkError += MqttClient_OnNetworkError;
                mqttClient.OnMqttMessageReceived += MqttClient_OnMqttMessageReceived;

                var connect = await mqttClient.ConnectServerAsync();
                if (connect.IsSuccess)
                {
                    SetMQTTConnected();
                    _timerFirstConnect.Enabled = false;
                }
                else
                {
                    SetMQTTReconnecting();
                    _timerFirstConnect.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                SetMQTTReconnecting();
                _timerFirstConnect.Enabled = true;
            }
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            _timerFirstConnect.Enabled = true;
            SetMQTTReconnecting();
        }


        private void MqttClient_OnMqttMessageReceived(MqttClient client, string topic, byte[] payload)
        {
            try
            {
                // get payload
                string ReceivedMessage = Encoding.UTF8.GetString(payload);

                // get topic name
                string TopicReceived = topic;

                TxtTopicNamereceived.Invoke(() => TxtTopicNamereceived.Text = TopicReceived);

                // Show message
                RTSubscribeJson.Invoke(() => RTSubscribeJson.Text = ReceivedMessage);
            }
            catch { }
        }

        private void MqttClient_OnNetworkError(object sender, EventArgs e)
        {
            LblMqttStatus.Invoke(() => LblMqttStatus.Text = "Reconnecting");
            _timerFirstConnect.Enabled = true;
        }

        private void SetMQTTConnected()
        {
            LblMqttStatus.Invoke(() => LblMqttStatus.Text = "Connected");
            this.Invoke((MethodInvoker)delegate
            {
                BtnConnect.Enabled = false;
                TxtMqttBroker.Enabled = false;
                GBSubscribeTopic.Enabled = true;
                RTPublishJson.Enabled = true;
                RTPublishJson.ReadOnly = false;
            });
        }
        private void SetMQTTReconnecting()
        {
            LblMqttStatus.Invoke(() => LblMqttStatus.Text = "Reconnecting");
        }

        private async void BtnSubscribe_Click(object sender, EventArgs e)
        {
            try
            {
                await mqttClient.SubscribeMessageAsync(MtxtTopicSubscribe.Text);

                LBTopicSubscribe.Items.Add(MtxtTopicSubscribe.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async void MbtnPublish_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                await mqttClient.PublishMessageAsync(
                   new MqttApplicationMessage()
                   {
                       Topic = MtxtTopicPublish.Text.Trim(),
                       QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                       Payload = Encoding.ASCII.GetBytes(RTPublishJson.Text),
                       Retain = false
                   }
               );

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
