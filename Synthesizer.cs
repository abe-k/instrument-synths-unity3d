using UnityEngine;
using System;

internal static class XorRand {
    private static uint y = 2463534242;
    private static void Advance() {
        y ^= (y << 13) & 0xFFFFFFFF;
        y ^= (y >> 17);
        y ^= (y << 5) & 0xFFFFFFFF;
    }
    public static float value() {
        Advance();
        return (Convert.ToInt64(y) - 2147483648) / 2147483647.0f;
    }
}
 

internal class SynthInstrument {
    protected int sr;
    public virtual void ReadSamples(float[] data, int channels) {}
}

internal class SynthGuitar : SynthInstrument {
    private double inc;
    private double phase;
    private int pos;
    
    private float[] buf = new float[256];
    
    public SynthGuitar(int sampleRate, double frequency) {
        sr = sampleRate;
        inc = frequency*256/sr;
        phase = 0;
        pos = 0;
        for (var i = 0; i < buf.Length; i++) {
            buf[i] = (float) (XorRand.value() + XorRand.value() - 1);
        }
    }
    
    public override void ReadSamples(float[] data, int channels) {
        for (var i = 0; i < data.Length; i += channels) {
            buf[pos] = (buf[pos] + buf[(pos-1) & 0xFF])/2;
            pos += 1;
            pos &= 0xFF;
            float val = (float)(buf[(int) Math.Floor(phase)]*(1-(phase-Math.Floor(phase))) +
                buf[((int) (Math.Floor(phase)+1))&0xFF]*(phase-Math.Floor(phase)));
            phase += inc;
            if (phase >= 256.0) {
                phase -= 256.0;
            }
            for (var c = 0; c < channels; c++) {
                data[i + c] += val/16;
            }
        }
    }
}

internal class SynthKickDrum : SynthInstrument {
    private double phase;
    private int pos;
    
    public SynthKickDrum(int sampleRate) {
        sr = sampleRate;
        phase = 0.0;
        pos = 0;
    }
    
    public override void ReadSamples(float[] data, int channels) {
        for (var i = 0; i < data.Length; i += channels) {
            double freq = 150*Math.Exp(-25.0*pos/sr)+50;
            phase += freq/sr;
            double ampl = Math.Exp(-8.0*pos/sr);
            float val = (float) (ampl*(Math.Sin(2*Math.PI*phase) )+ ampl*ampl*ampl*0.1*(XorRand.value()-XorRand.value()));
            pos += 1;
            for (var c = 0; c < channels; c++) {
                data[i + c] += val/2;
            }
        }
    }
}

internal class SynthFMPiano : SynthInstrument {
    private float fm = 0;
    private float phase = 0;
    private float freq;
    private int pos;
    
    public SynthFMPiano(int sampleRate, float frequency) {
        sr = sampleRate;
        freq = frequency;
    }
    
    public override void ReadSamples(float[] data, int channels) {
        for (var i = 0; i < data.Length; i += channels) {
            fm += 1.0f/sr * freq * 3f;
            phase += 1.0f/sr * freq + (Mathf.Exp(-1f*pos/sr) * Mathf.Sin(2*Mathf.PI*fm))/20;
            float val = (1 - Mathf.Exp(-60.0f*pos/sr)) * Mathf.Exp(-1.0f*pos/sr) * (Mathf.Sin(2*Mathf.PI*phase) + 0.01f * XorRand.value() + 1f * Mathf.Sin(2*Mathf.PI*pos*freq*1.01f/sr));
            pos += 1;
            for (var c = 0; c < channels; c++) {
                data[i + c] += val/2;
            }
        }
    }
}

public class Synthesizer : MonoBehaviour {
    
    private SynthInstrument[] voices = new SynthInstrument[4];
    private int voiceIndex = 0;
        
    public void Guitar(double frequency) {
        voices[voiceIndex] = new SynthGuitar(AudioSettings.outputSampleRate, frequency);
        voiceIndex += 1;
        if (voiceIndex >= voices.Length) {
            voiceIndex = 0;
        }
    }
    public void Piano(double frequency) {
        voices[voiceIndex] = new SynthFMPiano(AudioSettings.outputSampleRate, (float) frequency);
        voiceIndex += 1;
        if (voiceIndex >= voices.Length) {
            voiceIndex = 0;
        }
    }
    
    public void KickDrum() {
        voices[voiceIndex] = new SynthKickDrum(AudioSettings.outputSampleRate);
        voiceIndex += 1;
        if (voiceIndex >= voices.Length) {
            voiceIndex = 0;
        }
    }
    
    public void OnAudioFilterRead(float[] data, int channels) {
        for (var i = 0; i < voices.Length; i += 1) {
            if (voices[i] != null) {
                voices[i].ReadSamples(data, channels);
            }
        }
    }
} 
