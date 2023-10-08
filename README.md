# FFTMonitor-using-NAudio
This program reads the microphone, transforming the data into frequencies via the FFT transformations of NAudio. The frequencies are displayed in a ScottPlot diagram. I am using NAudio 2.2.1.0 and ScottPlot 4.1.68.0.

To understand the significance of this program, you'll need to compare it with my other project FFTMonitor-using-FFTSharp. To start with, I removed one library from the program, FFTSharp. Instead, I use the same library to read the microphone and produce the Fast Fourier Transformation. Much of the inspiration for this program comes from [BigPino67-TV/MicrophoneSpectrumAnalyzer](https://github.com/BigPino67-TV/MicrophoneSpectrumAnalyzer)

Very little in this program is identical to the approach taken in FFTMonitor-using-FFTSharp. Here are some examples:
* In this program, the microphone produces 16-bit integers, not 32-bit doubles. Already here, the program is pumping around less data.
* This program makes use of a HammingWindow, not a square window.
* No official library for Complex numbers is used. This is less correct regarding math but considerably more efficient. The official library for Complex numbers is immutable, meaning the library is rapidly creating new Complex number structures, and the garbage collector needs to discard these. If things are computationally intense, then this can make the program stalling.

I have found very few examples of how to use the FFT transformations built into NAudio. The program made by BigPino67 is the only complete program I could find. Many thanks to PigPino67 for providing that information! The sample was convoluted and claimed to use Accord, but with closer inspection, it turned out to be false, it used NAudio! BigPino67 used an algorithm for calculating the magnitude that did not add up quite well, but that was easily fixed.

Enjoy!

Regards,
Jens Malmgren
