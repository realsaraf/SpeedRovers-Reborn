using System;
using System.IO;
using UnityEngine;

namespace SpeedRovers.Reborn
{
    // Only installed by the explicit audio review command-line mode.
    // Receives the listener mix on the audio thread; no Unity calls there.
    public sealed class RaceAudioCapture : MonoBehaviour
    {
        readonly object sync=new object();
        float[] samples;
        int count, channels=2, rate;
        public void Begin(int seconds)
        {
            rate=AudioSettings.outputSampleRate;
            lock(sync) {samples=new float[rate*seconds*8];count=0;}
        }
        void OnAudioFilterRead(float[] data,int channelCount)
        {
            lock(sync)
            {
                if(samples==null)return;
                channels=channelCount;
                int length=Math.Min(data.Length,samples.Length-count);
                Array.Copy(data,0,samples,count,length);count+=length;
            }
        }
        public void Save(string path)
        {
            lock(sync)
            {
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);
                    writer.Write((short)1);writer.Write((short)channels);writer.Write(rate);
                    writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                    for(int i=0;i<count;i++)writer.Write((short)(Mathf.Clamp(samples[i],-1,1)*32767));
                }
                Debug.Log("Audio review captured "+count+" samples at "+rate+" Hz, "+channels+" channels.");
                samples=null;
            }
        }
    }
}
