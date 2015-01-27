using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using common.message;
using Google.ProtocolBuffers;
using NAudio.Wave;

namespace CloudX.utils
{
    internal class AudioCaptureUtils
    {
        private Stream outputStream;
        private IWaveIn waveIn;

        public void StartCapture(Stream stream)
        {
            // can't set WaveFormat as WASAPI doesn't support SRC
            waveIn = new WasapiLoopbackCapture();

            outputStream = stream;

            //Console.WriteLine(waveIn.WaveFormat);

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;
            waveIn.StartRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded != 0)
            {
                var buffer = new byte[e.Buffer.Length*2];
                int readSize = WaveFloatTo16(e.Buffer, e.BytesRecorded, buffer);
                if (outputStream != null)
                    lock (outputStream)
                    {
                        DataPacket.CreateBuilder()
                            .SetUnixTimeStamp(0)//TODO set time stamp
                            .SetDataPacketType(DataPacket.Types.DataPacketType.Audio)
                            .SetAudio(
                                Audio.CreateBuilder()
                                    .SetSound(ByteString.CopyFrom(GZipCompress(buffer, readSize))) //compress
                                    .Build()
                            ).Build()
                            .WriteDelimitedTo(outputStream);

                        //outputStream.Flush();
                    }
                else
                {
                    StopRecording();
                }
                //Console.WriteLine("recording");
            }
        }


        private static int WaveFloatTo16(byte[] sourceBuffer, int numBytes, byte[] destBuffer)
        {
            var sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            var destWaveBuffer = new WaveBuffer(destBuffer);

            int sourceSamples = numBytes/4;
            int destOffset = 0;
            for (int sample = 0; sample < sourceSamples; sample++)
            {
                float sample32 = sourceWaveBuffer.FloatBuffer[sample];
                // clip
                if (sample32 > 1.0f)
                    sample32 = 1.0f;
                if (sample32 < -1.0f)
                    sample32 = -1.0f;
                destWaveBuffer.ShortBuffer[destOffset++] = (short) (sample32*32767);
            }

            return sourceSamples*2;
        }

        public void StopRecording()
        {
            Debug.WriteLine("StopRecording");
            waveIn.StopRecording();
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (waveIn != null) // working around problem with double raising of RecordingStopped
            {
                waveIn.Dispose();
                waveIn = null;
            }
        }

        private static byte[] GZipCompress(byte[] source, int length)
        {
            var memoryStream = new MemoryStream();
            var stream = new GZipStream(memoryStream, CompressionMode.Compress);
            stream.Write(source, 0, length);
            stream.Flush();
            stream.Close();

            //Console.WriteLine("GZip : {0}", memoryStream.ToArray().Length);

            return memoryStream.ToArray();
        }
    }
}