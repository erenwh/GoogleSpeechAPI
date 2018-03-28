using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Grpc.Auth;
using System.IO;



namespace GoogleSpeechAPI
{
	
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private BufferedWaveProvider bwp;
		
        WaveIn waveIn;
        WaveOut waveOut;
        WaveFileWriter writer;
        WaveFileReader reader; 
        string output = "audio.raw";

        public MainWindow()
        {
            InitializeComponent();

            waveOut = new WaveOut();
            waveIn = new WaveIn();

            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(waveIn_DataAvailable);
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            bwp = new BufferedWaveProvider(waveIn.WaveFormat);
            bwp.DiscardOnBufferOverflow = true;

			
            btnRecordVoice.IsEnabled = true;
            btnSave.IsEnabled = false;
            btnSpeechInfo.IsEnabled = false;



        }
        
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);

        }


        private void waveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            waveOut.Stop();
            reader.Close();
            //reader = null;
        }
		

		private void btnRecordVoice_Click(object sender, RoutedEventArgs e)
		{
			if (NAudio.Wave.WaveIn.DeviceCount < 1)
			{
				Console.WriteLine("No microphone!");
				return;
			}
			if (waveIn == null)
			{
				waveIn = new WaveIn();
			}
			waveIn.StartRecording();

			btnRecordVoice.IsEnabled = false;
			btnSave.IsEnabled = true;
			btnSpeechInfo.IsEnabled = false;
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			waveIn.StopRecording();
			
			if (File.Exists("audio.raw"))
				File.Delete("audio.raw");


			writer = new WaveFileWriter(output, waveIn.WaveFormat);

			btnRecordVoice.IsEnabled = false;
			btnSave.IsEnabled = false;
			btnSpeechInfo.IsEnabled = true;

			byte[] buffer = new byte[bwp.BufferLength];
			int offset = 0;
			int count = bwp.BufferLength;

			var read = bwp.Read(buffer, offset, count);
			if (count > 0)
			{
				writer.Write(buffer, offset, read);
			}

			//waveIn.Dispose();
			//waveIn = null;
			writer.Close();
			//writer = null;

			reader = new WaveFileReader("audio.raw"); // (new MemoryStream(bytes));
			waveOut.Init(reader);
			waveOut.PlaybackStopped += new EventHandler<StoppedEventArgs>(waveOut_PlaybackStopped);
			waveOut.Play();
			
		}

		private void btnSpeechInfo_Click(object sender, RoutedEventArgs e)
		{
			btnRecordVoice.IsEnabled = true;
			btnSave.IsEnabled = false;
			btnSpeechInfo.IsEnabled = false;


			var speech = SpeechClient.Create();
			var response = speech.Recognize(new RecognitionConfig()
			{
				Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
				SampleRateHertz = 16000,
				LanguageCode = "en",
			}, RecognitionAudio.FromFile("audio.raw"));


			textBox1.Text = "";

			foreach (var result in response.Results)
			{
				foreach (var alternative in result.Alternatives)
				{
					textBox1.Text = textBox1.Text + " " + alternative.Transcript;
				}
			}

			if (textBox1.Text.Length == 0)
				textBox1.Text = "No Data ";

			/*if (File.Exists("audio.raw"))
			{

				var speech = SpeechClient.Create();
				var response = speech.Recognize(new RecognitionConfig()
				{
					Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
					SampleRateHertz = 16000,
					LanguageCode = "en",
				}, RecognitionAudio.FromFile("audio.raw"));


				textBox1.Text = "";

				foreach (var result in response.Results)
				{
					foreach (var alternative in result.Alternatives)
					{
						textBox1.Text = textBox1.Text + " " + alternative.Transcript;
					}
				}

				if (textBox1.Text.Length == 0)
					textBox1.Text = "No Data ";

			}
			else
			{

				textBox1.Text = "Audio File Missing ";

			}
			*/
		}

		private void btnPlayAudio_Click(object sender, RoutedEventArgs e)
		{
			if (File.Exists("audio.raw"))
			{
				reader = new WaveFileReader("audio.raw");
				waveOut.Init(reader);
				waveOut.Play();
			}
			else
			{
				MessageBox.Show("No Audio File Found");
			}
		}
	}
}
