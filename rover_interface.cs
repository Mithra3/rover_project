*****************************************************************
// C# Application. Note - doesn't include the auto generated .designer or Program.cs code for winforms. 

using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using static System.Net.WebRequestMethods;
using static System.Net.Mime.MediaTypeNames;

namespace RoverInterface2 {
    public partial class RoverInterface : Form {
        public RoverInterface() {
            InitializeComponent();

        }

        // parameterized Url and port for the Rover server
        public static string baseUrl = "http://192.168.1.197:8080";

        private async void startPathButton_Click(object sender, EventArgs e) {
            
            string endpoint = "/findPath";
            string url = baseUrl + endpoint;

            string xValue = xTextBox.Text;
            string yValue = yTextBox.Text;

            string message = (xValue + " " + yValue);

            await Task.Run(() => {

                try { 
                    using (var client = new WebClient()) {
                        string response = client.UploadString(url, message);
                        Invoke((MethodInvoker)delegate {
                            notificationLabel.Text = response;
                        });
                    }
                } catch {
                    Invoke((MethodInvoker)delegate {
                        notificationLabel.Text = "Problem communicating with Rover";
                    });
                }
            });
        }

        private async void initialiseGpios_Click(object sender, EventArgs e) {

            initialiseGpios.Enabled = false;

            await Task.Run(() => {

                string endpoint = "/initialise";
                string url = baseUrl + endpoint;
                string message = ("/initialise");
            
                try { 
                    using (var client = new WebClient()) {
                        string response = client.UploadString(url, "POST", message);
                        Invoke((MethodInvoker)delegate {
                            notificationLabel.Text = response;
                        });
                    }
                } catch {
                    Invoke((MethodInvoker)delegate {
                        notificationLabel.Text = "Problem communicating with Rover";
                        initialiseGpios.Enabled = true;
                    });
                }
            });
        }

        private void motors(string url, string message) {

            try { 
                using (var client = new WebClient()) {
                    string response = client.UploadString(url, "POST", message);
                    notificationLabel.Text = response;
                }
            } catch {
                notificationLabel.Text = "Problem communicating with Rover";
            }
        }

        // used for real time control 
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {

            string endpoint = "";
            string url = "";

            if(keyData == Keys.P) {
                endpoint = "/stop";
                url = baseUrl + endpoint;
                motors(url, "stop");
                return true;
            }
            if(keyData == Keys.Up) {
                endpoint = "/forward";
                url = baseUrl + endpoint;
                motors(url, "forward");
                return true;
            }
            if(keyData == Keys.Down) {
                endpoint = "/reverse";
                url = baseUrl + endpoint;
                motors(url, "reverse");
                return true;
            }
            if(keyData == Keys.Left) {
                endpoint = "/left";
                url = baseUrl + endpoint;
                motors(url,"left");
                return true;
            }
            if(keyData == Keys.Right) {
                endpoint = "/right";
                url = baseUrl + endpoint;
                motors(url,"right");
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void updateNavCamButton_Click(object sender, EventArgs e) {

            HttpClient httpClient = new HttpClient();
            string endpoint = "/image";
            string url = baseUrl + endpoint;
            string message = "howaya";

            try { 
                var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                var imageData = await response.Content.ReadAsByteArrayAsync();
                    navCamView.Image = System.Drawing.Image.FromStream(new System.IO.MemoryStream(imageData));
            } catch {

            }
        }

        private void exitProgramButton_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void stopButton_Click(object sender, EventArgs e) {

            string endpoint = "/stop";
            string url = baseUrl + endpoint;     
            motors(url, "stop");

        }

        private async void updateMap_Click(object sender, EventArgs e) {

            HttpClient httpClient = new HttpClient();

            string endpoint = "/map";
            string url = baseUrl + endpoint;  
            string message = "hello";
            string mapData = "";

            try { 
                var response = await httpClient.GetAsync(url);
                mapData = await response.Content.ReadAsStringAsync();
                MessageBox.Show(mapData);
            } catch {
                MessageBox.Show("Problem updating map. No data received", "Communication error", MessageBoxButtons.OK);
                return;
            }

            // used for debug
            /* 
            using (StreamWriter writer = new StreamWriter("lines.txt")) {
                for(int i = 0; i < 10; i++) {
                    writer.WriteLine(lines[i]);
                }
            }
            */

            // map data comes as "   theta: 00.0 Dist: 00.0" 
            string[] lines = mapData.Split('\n');

            float[] thetas = new float[lines.Length];
            float[] distances = new float[lines.Length];
            float theta;
            float distance;

            // the data is a bit messy, it needs whitespaces and the odd 'S' removed
            for (int i = 0; i < lines.Length; i++) {

                string[] values = lines[i].Trim().Split(' ');
                // avoid empty spaces
                if (values.Length >= 4) {
            //        MessageBox.Show(lines[i]);
                    theta = float.Parse(values[1]);
                    distance = float.Parse(values[3]);
                    thetas[i] = theta;
                    distances[i] = distance;
                }
            }

          //  Bitmap bmp = new Bitmap(mapPictureBox.Image);

            Graphics graphics = mapPictureBox.CreateGraphics();
            PointF center = new PointF(mapPictureBox.Width / 2, mapPictureBox.Height / 2);
            float startAngle = 0;
            float sweepAngle = 0;


            for (int i = 0; i < thetas.Length; i++) {

                float sizeOfBox = 1200;
                float diameter = distances[i] * 2;

                RectangleF rectangle = new RectangleF(center.X - sizeOfBox, 
                    center.Y - sizeOfBox, diameter, diameter);

                sweepAngle = thetas[i]; // degrees
                startAngle = startAngle + sweepAngle;

           //   graphics.DrawEllipse(Pens.Black, rectangle, startAngle, sweepAngle);
                graphics.DrawArc(Pens.Black, rectangle, startAngle, sweepAngle);
                mapPictureBox.Refresh();

            }
        }

        private async void startMonitoringButton_Click(object sender, EventArgs e) {

            startMonitoringButton.Enabled = false;
            string intervalStr = dataIntervalBox.Text;
            int interval;

            if(!int.TryParse(intervalStr, out interval)) {
                MessageBox.Show("Enter an integer into the text box for time between readings", "No interval input", MessageBoxButtons.OK);
                startMonitoringButton.Enabled = true;
                return;
            }
            interval = interval*1000;

            await Task.Run(() => {
                
                HttpClient httpClient = new HttpClient();

                string endpoint = "/data";
                string url = baseUrl + endpoint;  
                string message = "howaya";

                string data = "";
                string dataString = "";

                Series airTempSeries = new Series("Temperature");
                airTempSeries.ChartType = SeriesChartType.Spline;

                Series airHumidSeries = new Series("Humidity");
                airHumidSeries.ChartType = SeriesChartType.Spline;

                Series airPresSeries = new Series("Air Pressure");
                airPresSeries.ChartType = SeriesChartType.Spline;

                Series airQualSeries = new Series("Air Quality");
                airQualSeries.ChartType = SeriesChartType.Spline;

                Invoke((MethodInvoker)delegate {

                    airTempChart.Series.Clear();
                    airTempChart.Series.Add(airTempSeries);
                    airTempChart.ChartAreas[0].AxisY.Maximum = 80;


                    humidityChart.Series.Clear();
                    humidityChart.Series.Add(airHumidSeries);


                    airPresChart.Series.Clear();
                    airPresChart.Series.Add(airPresSeries);


                    airQualityChart.Series.Clear();
                    airQualityChart.Series.Add(airQualSeries);

                    humidityChart.ChartAreas[0].AxisY.Maximum = 100;
                    airPresChart.ChartAreas[0].AxisY.Maximum = 1100;
                    airPresChart.ChartAreas[0].AxisY.Minimum = 900;
                    airQualityChart.ChartAreas[0].AxisY.Maximum = 200000;
                });
                
                float[] airTemp = new float[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
                float[] airHumid = new float[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
                float[] airPres = new float[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
                float[] airQual = new float[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}; 

                while(true) { 

                    try { 
                        using (var client = new WebClient()) {
                            data = client.UploadString(url, "POST", message);
                            Invoke((MethodInvoker)delegate {
                                notification2Label.Text = "Receiving weather data";
                            });
                        }
                    } catch {
                        Invoke((MethodInvoker)delegate {
                            notification2Label.Text = "Not receiving weather data";
                        });
                    }
                    dataString = data.ToString();
                                
                    string[] values = dataString.Trim().Split(' ');

                    float newTemp = float.Parse(values[1]);
                    float newPressure = float.Parse(values[4]);
                    float newAltitude = float.Parse(values[7]);
                    float newHumidity = float.Parse(values[10]);
                    float newAirQuality = float.Parse(values[13]);              



                    for(int i = 29; i > 0; i--) {
                        airTemp[i] = airTemp[i - 1];
                        airHumid[i] = airHumid[i - 1];
                        airPres[i] = airPres[i - 1];
                        airQual[i] = airQual[i - 1];
                    }
                    airTemp[0] = newTemp;
                    airHumid[0] = newHumidity;
                    airPres[0] = newPressure;
                    airQual[0] = newAirQuality;

                    Invoke((MethodInvoker)delegate {


                        airTempSeries.Points.Clear();
                        airHumidSeries.Points.Clear();
                        airPresSeries.Points.Clear();
                        airQualSeries.Points.Clear();     
                        
                        tempReadLabel.Text = newTemp.ToString();
                        humidityLabel.Text = newHumidity.ToString();
                        pressureReadLabel.Text = newPressure.ToString();
                        airQualityLabel.Text = newAirQuality.ToString();



                        for (int i = 0; i < 30; i++) {
                            airTempSeries.Points.AddXY(i, airTemp[i]);
                            airHumidSeries.Points.AddXY(i, airHumid[i]);
                            airPresSeries.Points.AddXY(i, airPres[i]);
                            airQualSeries.Points.AddXY(i, airQual[i]);
                        }
                    });
                    Thread.Sleep(interval);

                }
            });
        }

        private void tempReadLabel_Click(object sender, EventArgs e) {

        }

        private async void downloadDataButton_Click(object sender, EventArgs e) {

            await Task.Run(() => {
                
                HttpClient httpClient = new HttpClient();
   
                string endpoint = "/allData";
                string url = baseUrl + endpoint;

                string message = "howaya";
                string allData = "";
                string dataString = "";
                try { 
                    using (var client = new WebClient()) {
                        allData = client.UploadString(url, "POST", message);
                        MessageBox.Show(allData, "Data is", MessageBoxButtons.OK);
                    }
                } catch {
                    
                }

            });
        }
    }
}
