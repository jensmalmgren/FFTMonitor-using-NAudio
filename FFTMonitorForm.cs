using NAudio.Wave;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastFourierMonitor
{
	public partial class FFTMonitorForm : Form
	{
		double m_dFrequencyResolutionInHz;
		private List<WaveInCapabilities> m_ListDevices = new List<WaveInCapabilities> ();
		private WaveIn _wi;
		private BufferedWaveProvider _bwp;

		private int RATE = 44100; // sample rate of the sound card
		private int BUFFERSIZE = (int)Math.Pow(2, 11); // must be a multiple of 2

		private double[] m_dataFft_ForPlot;
		public FFTMonitorForm()
		{
			InitializeComponent();

			int waveInDevices = WaveIn.DeviceCount;
			for (int waveInDeviceNumber = 0; waveInDeviceNumber < waveInDevices; waveInDeviceNumber++)
			{
				m_ListDevices.Add(WaveIn.GetCapabilities(waveInDeviceNumber));
			}

			if (m_ListDevices.Count == 0)
			{
				MessageBox.Show("Could not detect a microphone");
				Close();
			}

			_wi = new WaveIn();
			_wi.DeviceNumber = 0; 
			_wi.WaveFormat = new NAudio.Wave.WaveFormat(RATE, 1);
			_wi.BufferMilliseconds = (int)((double)BUFFERSIZE / (double)RATE * 1000.0);
			_wi.DataAvailable += new EventHandler<WaveInEventArgs>(AudioDataAvailable);
			_bwp = new BufferedWaveProvider(_wi.WaveFormat);
			_bwp.BufferLength = BUFFERSIZE * 2;
			_bwp.DiscardOnBufferOverflow = true;

			try
			{
				_wi.StartRecording();
			}
			catch
			{
				string msg = "Could not record from audio device!";
				MessageBox.Show(msg, "ERROR");
			}

			m_dataFft_ForPlot = new double[512];
			for (int _i = 0; _i < m_dataFft_ForPlot.Length; _i++)
			{
				m_dataFft_ForPlot[_i] = 0;
			}
			m_dFrequencyResolutionInHz = _wi.BufferMilliseconds;
			formsPlot1.Plot.AddSignal(m_dataFft_ForPlot, 1.0 / m_dFrequencyResolutionInHz);
			formsPlot1.Plot.YLabel("Spectral Power");
			formsPlot1.Plot.XLabel("Frequency (kHz)");
			formsPlot1.Plot.Title($"{_wi.WaveFormat.Encoding} ({_wi.WaveFormat.BitsPerSample}-bit) {_wi.WaveFormat.SampleRate} KHz");
			formsPlot1.Plot.SetAxisLimits(0, 10000, 0, 500);
			formsPlot1.Refresh();
		} // public FFTMonitorForm()

		void AudioDataAvailable(object sender, WaveInEventArgs args)
		{
			SetDataFFT(args);
		} // AudioDataAvailable()

		private void SetDataFFT(WaveInEventArgs args)
		{
			int bytesPerSample = _wi.WaveFormat.BitsPerSample / 8;
			int samplesRecorded = args.BytesRecorded / bytesPerSample;
			Int16[] dataPcm = new Int16[samplesRecorded];
			for (int i = 0; i < samplesRecorded; i++)
			{
				dataPcm[i] = BitConverter.ToInt16(args.Buffer, i * bytesPerSample);
			}

			// the PCM size to be analyzed with FFT must be a power of 2
			int fftPoints = 2;
			while (fftPoints * 2 <= dataPcm.Length)
			{
				fftPoints *= 2;
			}

			// apply a Hamming window function as we load the FFT array then calculate the FFT
			NAudio.Dsp.Complex[] fftFull = new NAudio.Dsp.Complex[fftPoints];
			for (int i = 0; i < fftPoints; i++)
			{
				fftFull[i].X = (float)(dataPcm[i] * NAudio.Dsp.FastFourierTransform.HammingWindow(i, fftPoints));
			}
			NAudio.Dsp.FastFourierTransform.FFT(true, (int)Math.Log(fftPoints, 2.0), fftFull);

			double[] dataFft = new double[fftPoints / 2];
			//byte[] data = new byte[fftPoints / 2];

			for (int i = 0; i < fftPoints / 2; i++)
			{
				dataFft[i] = Hypot(fftFull[i].X, fftFull[i].Y); ;
			}

			if (dataFft.Length > m_dataFft_ForPlot.Length)
			{
				Trace.WriteLine("Plotbuffer too small");
			}
			dataFft.CopyTo(m_dataFft_ForPlot, 0);
			for (int _i = dataFft.Length; _i< m_dataFft_ForPlot.Length; _i++)
			{
				m_dataFft_ForPlot[_i] = 0;
			}
			RefreshThePlot(formsPlot1);
		} // SetDataFFT

		private delegate void RefreshRequestDelegate(FormsPlot ip_FormsPlot);
		public static void RefreshThePlot(FormsPlot ip_FormsPlot)
		{
			if (ip_FormsPlot.InvokeRequired)
			{
				ip_FormsPlot.Invoke(new RefreshRequestDelegate(RefreshThePlot), new object[] { ip_FormsPlot });
			}
			else
			{
				ip_FormsPlot.RefreshRequest();
			}
		} // RefreshThePlot()

		private double Hypot(double a, double b)
		{
			// Using
			//   sqrt(a^2 + b^2) = |a| * sqrt(1 + (b/a)^2)
			// we can factor out the larger component to dodge overflow even when a * a would overflow.

			a = Math.Abs(a);
			b = Math.Abs(b);

			double small, large;
			if (a < b)
			{
				small = a;
				large = b;
			}
			else
			{
				small = b;
				large = a;
			}

			if (small == 0.0)
			{
				return (large);
			}
			else if (double.IsPositiveInfinity(large) && !double.IsNaN(small))
			{
				// The NaN test is necessary so we don't return +inf when small=NaN and large=+inf.
				// NaN in any other place returns NaN without any special handling.
				return (double.PositiveInfinity);
			}
			else
			{
				double ratio = small / large;
				return (large * Math.Sqrt(1.0 + ratio * ratio));
			}

		} // Hypot() ... from the Complex lib

	} // class FFTMonitorForm
}
