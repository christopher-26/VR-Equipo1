import math
import struct
import wave
import random
import os

SAMPLE_RATE = 44100

def write_wav(filename, samples, num_channels=2, sample_rate=SAMPLE_RATE):
    """Writes floating point samples (-1.0 to 1.0) to a 16-bit PCM WAV file."""
    os.makedirs(os.path.dirname(filename), exist_ok=True)
    with wave.open(filename, 'wb') as wav_file:
        wav_file.setnchannels(num_channels)
        wav_file.setsampwidth(2) # 16 bits = 2 bytes
        wav_file.setframerate(sample_rate)
        
        raw_data = bytearray()
        for s in samples:
            # Clamp to [-1.0, 1.0]
            val = max(-1.0, min(1.0, s))
            int_val = int(val * 32767.0)
            raw_data.extend(struct.pack('<h', int_val))
        wav_file.writeframes(raw_data)
    print(f"Generated: {filename} ({len(samples) // (num_channels * sample_rate):.1f}s)")

def adsr(t, duration, a=0.05, d=0.1, s=0.7, r=0.2):
    """ADSR envelope generator."""
    if t < 0 or t > duration:
        return 0.0
    if t < a:
        return t / a if a > 0 else 1.0
    t -= a
    if t < d:
        return 1.0 - (1.0 - s) * (t / d) if d > 0 else s
    rel_start = duration - r
    if t < (rel_start - a - d):
        return s
    # Release phase
    t_rel = t - (rel_start - a - d)
    return s * max(0.0, 1.0 - t_rel / r) if r > 0 else 0.0

def synth_sine(freq, t, phase=0.0):
    return math.sin(2.0 * math.pi * freq * t + phase)

def synth_saw(freq, t, harmonics=6):
    """Band-limited saw wave."""
    val = 0.0
    for h in range(1, harmonics + 1):
        val += (math.sin(2.0 * math.pi * freq * h * t) / h)
    return val * (2.0 / math.pi)

def synth_pulse(freq, t, duty=0.3, harmonics=6):
    """Pulse wave."""
    val = 0.0
    for h in range(1, harmonics + 1):
        amp = math.sin(h * math.pi * duty) / h
        val += math.sin(2.0 * math.pi * freq * h * t) * amp
    return val * 1.5

# Note to frequency helper
NOTE_MAP = {
    'C2': 65.41, 'D2': 73.42, 'Eb2': 77.78, 'E2': 82.41, 'F2': 87.31, 'G2': 98.00, 'A2': 110.00, 'Bb2': 116.54, 'B2': 123.47,
    'C3': 130.81, 'D3': 146.83, 'Eb3': 155.56, 'E3': 164.81, 'F3': 174.61, 'G3': 196.00, 'A3': 220.00, 'Bb3': 233.08, 'B3': 246.94,
    'C4': 261.63, 'D4': 293.66, 'Eb4': 311.13, 'E4': 329.63, 'F4': 349.23, 'G4': 392.00, 'A4': 440.00, 'Bb4': 466.16, 'B4': 493.88,
    'C5': 523.25, 'D5': 587.33, 'Eb5': 622.25, 'E5': 659.25, 'F5': 698.46, 'G5': 783.99, 'A5': 880.00, 'Bb5': 932.33, 'B5': 987.77,
    'C6': 1046.50
}

def generate_investigation_music(out_path):
    """Generate 32s loopable Cyber Investigation Theme (D minor)."""
    duration = 32.0
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * (total_frames * 2) # Stereo
    
    # Chord progression: 4 bars Dm, 4 bars Bb, 4 bars Gm, 4 bars A7/Dm (each bar 2s = 120BPM)
    # 8 beats per chord = 4.0s per chord -> 8 chords = 32s
    chords = [
        ('Dm', ['D3', 'F3', 'A3', 'D4'], 'D2'),
        ('Bb', ['Bb2', 'D3', 'F3', 'Bb3'], 'Bb2'),
        ('Gm', ['G2', 'Bb2', 'D3', 'G3'], 'G2'),
        ('A7', ['A2', 'C#3' if 'C#3' in NOTE_MAP else 'A2', 'E3', 'G3'], 'A2'),
        ('Dm', ['D3', 'F3', 'A3', 'D4'], 'D2'),
        ('Bb', ['Bb2', 'D3', 'F3', 'Bb3'], 'Bb2'),
        ('Gm', ['G2', 'Bb2', 'D3', 'G3'], 'G2'),
        ('Asus', ['A2', 'D3', 'E3', 'A3'], 'A2')
    ]
    chord_len = duration / len(chords) # 4.0s each
    
    # Arpeggio pattern (16th notes = 0.125s)
    arp_notes = ['D4', 'F4', 'A4', 'D5', 'C5', 'A4', 'F4', 'D4', 
                 'Bb4', 'D5', 'F5', 'D5', 'A4', 'G4', 'F4', 'E4']
    arp_step = 0.125
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        chord_idx = min(len(chords) - 1, int(t / chord_len))
        _, chord_notes, bass_note = chords[chord_idx]
        t_in_chord = t % chord_len
        
        # 1. Sub-bass pulse
        bass_freq = NOTE_MAP.get(bass_note, 73.42)
        bass_env = 0.6 + 0.3 * math.sin(2.0 * math.pi * 2.0 * t) # 2 Hz gentle lfo
        bass = synth_sine(bass_freq, t) * 0.28 * bass_env + synth_saw(bass_freq, t, 3) * 0.10 * bass_env
        
        # 2. Atmospheric Synth Pad (warm detuned saw/sine with gentle filter)
        pad = 0.0
        for n in chord_notes:
            freq = NOTE_MAP.get(n, 220.0)
            pad += synth_sine(freq, t) * 0.05
            pad += synth_sine(freq * 1.004, t, 0.5) * 0.04 # chorus detune
        pad *= (0.7 + 0.3 * math.sin(2.0 * math.pi * 0.25 * t)) # slow pad swell
        
        # 3. Sequenced Cyber Arpeggio
        arp_idx = int(t / arp_step) % len(arp_notes)
        arp_t = (t % arp_step)
        arp_freq = NOTE_MAP.get(arp_notes[arp_idx], 440.0)
        arp_env = adsr(arp_t, arp_step, a=0.01, d=0.04, s=0.3, r=0.06)
        arp = (synth_sine(arp_freq, t) * 0.7 + synth_pulse(arp_freq, t, 0.2, 4) * 0.3) * arp_env * 0.16
        
        # 4. Subtle Digital Clock Tick / Percussion
        beat_t = t % 0.5 # 120 BPM quarter note
        tick_env = math.exp(-beat_t * 35.0)
        tick = synth_sine(1800.0, t) * 0.04 * tick_env
        
        # Panning
        l = bass * 0.9 + pad * 0.85 + arp * 0.7 + tick * 0.5
        r = bass * 0.9 + pad * 0.85 + arp * 1.0 + tick * 0.5
        
        # Seamless loop crossfade at edges (first/last 1.0s)
        fade = 1.0
        if t < 1.0:
            fade = t / 1.0
        elif t > duration - 1.0:
            fade = (duration - t) / 1.0
            
        samples[f * 2] = l * fade
        samples[f * 2 + 1] = r * fade

    # Re-apply loop wrap-around so start and end match perfectly
    cross_frames = int(SAMPLE_RATE * 1.0)
    for i in range(cross_frames):
        alpha = i / cross_frames
        # End blending into start
        samples[i * 2] += samples[(total_frames - cross_frames + i) * 2] * (1.0 - alpha)
        samples[i * 2 + 1] += samples[(total_frames - cross_frames + i) * 2 + 1] * (1.0 - alpha)
        
    write_wav(out_path, samples)

def generate_attack_music(out_path):
    """Generate 24s loopable Attack Tension Theme (Fast 128BPM, F minor)."""
    duration = 24.0
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * (total_frames * 2)
    
    bass_notes = ['F2', 'F2', 'Ab2', 'Eb2', 'Db2', 'Db2', 'Eb2', 'C2']
    step_time = 0.46875 # 128 BPM beat
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        beat_idx = int(t / step_time) % len(bass_notes)
        note_name = bass_notes[beat_idx]
        freq = NOTE_MAP.get(note_name, 87.31)
        
        t_in_beat = t % step_time
        env = adsr(t_in_beat, step_time, a=0.01, d=0.15, s=0.4, r=0.1)
        
        # Driving distorted saw bass
        raw_bass = synth_saw(freq, t, 8) + synth_pulse(freq * 0.5, t, 0.4, 4) * 0.8
        dist_bass = math.tanh(raw_bass * 1.8) * 0.25 * env
        
        # Urgent pulsing alarm / riser tone (440Hz <-> 880Hz)
        alarm_lfo = (math.sin(2.0 * math.pi * 4.0 * t) + 1.0) * 0.5 # 4 Hz pulse
        alarm = synth_saw(440.0 + alarm_lfo * 220.0, t, 4) * 0.08 * alarm_lfo
        
        # High cybernetic sequencer
        seq_step = step_time / 4.0
        seq_idx = int(t / seq_step) % 4
        seq_freq = freq * (2.0 if seq_idx % 2 == 0 else 3.0)
        seq_env = math.exp(-(t % seq_step) * 25.0)
        seq = synth_pulse(seq_freq, t, 0.2, 5) * 0.09 * seq_env
        
        l = dist_bass + alarm * 0.8 + seq * 0.6
        r = dist_bass + alarm * 0.5 + seq * 0.9
        
        fade = 1.0
        if t < 0.8: fade = t / 0.8
        elif t > duration - 0.8: fade = (duration - t) / 0.8
        
        samples[f * 2] = l * fade
        samples[f * 2 + 1] = r * fade
        
    write_wav(out_path, samples)

def generate_defense_music(out_path):
    """Generate 20s loopable Defense Victory Theme (Major, uplifting cyber-shield)."""
    duration = 20.0
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * (total_frames * 2)
    
    # C major -> G -> Am -> F
    prog = [
        (['C3', 'E3', 'G3', 'C4'], 'C2'),
        (['G2', 'B2', 'D3', 'G3'], 'G2'),
        (['A2', 'C3', 'E3', 'A3'], 'A2'),
        (['F2', 'A2', 'C3', 'F3'], 'F2')
    ]
    chord_len = duration / len(prog)
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        chord_idx = min(len(prog) - 1, int(t / chord_len))
        notes, bass_n = prog[chord_idx]
        
        # Warm heroic synth brass
        chord_val = 0.0
        for n in notes:
            fq = NOTE_MAP.get(n, 261.63)
            chord_val += synth_saw(fq, t, 5) * 0.06 + synth_sine(fq, t) * 0.08
        
        # Sub-bass
        bfq = NOTE_MAP.get(bass_n, 65.41)
        bass = synth_sine(bfq, t) * 0.22 + synth_sine(bfq * 2.0, t) * 0.1
        
        # Sparkling high chime arpeggios
        chime_step = 0.2
        chime_t = t % chime_step
        chime_idx = int(t / chime_step) % len(notes)
        chime_fq = NOTE_MAP.get(notes[chime_idx], 523.25) * 2.0
        chime_env = math.exp(-chime_t * 12.0)
        chime = synth_sine(chime_fq, t) * 0.12 * chime_env
        
        l = chord_val * 0.8 + bass * 0.9 + chime * 0.7
        r = chord_val * 0.8 + bass * 0.9 + chime * 1.0
        
        fade = 1.0
        if t < 0.8: fade = t / 0.8
        elif t > duration - 0.8: fade = (duration - t) / 0.8
        
        samples[f * 2] = l * fade
        samples[f * 2 + 1] = r * fade
        
    write_wav(out_path, samples)

def generate_menu_music(out_path):
    """Generate 20s loopable VR Menu Theme (Deep futuristic ambient)."""
    duration = 20.0
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * (total_frames * 2)
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        
        # Deep sub-drone in D (36.7Hz / 73.4Hz)
        drone = synth_sine(36.71, t) * 0.25 + synth_sine(73.42, t) * 0.15
        
        # Evolving harmonic resonances (D4, A4, D5)
        res_env = (math.sin(2.0 * math.pi * 0.1 * t) + 1.0) * 0.5
        reso = (synth_sine(293.66, t) * 0.08 + synth_sine(440.0, t) * 0.06 + synth_sine(587.33, t) * 0.05) * res_env
        
        # Digital heartbeat / telemetry blips (every 2.0s)
        blip_t = t % 2.0
        blip = 0.0
        if blip_t < 0.1:
            blip = synth_sine(1200.0, t) * math.exp(-blip_t * 40.0) * 0.08
        elif 0.2 < blip_t < 0.3:
            blip = synth_sine(1600.0, t) * math.exp(-(blip_t - 0.2) * 40.0) * 0.07
            
        l = drone + reso * 0.9 + blip * 0.7
        r = drone + reso * 0.8 + blip * 0.9
        
        fade = 1.0
        if t < 1.0: fade = t / 1.0
        elif t > duration - 1.0: fade = (duration - t) / 1.0
        
        samples[f * 2] = l * fade
        samples[f * 2 + 1] = r * fade
        
    write_wav(out_path, samples)

def generate_phone_ring(out_path):
    """Generate authentic office PBX phone ring (440Hz + 480Hz)."""
    duration = 3.2
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_frames # Mono
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        # Cadence: Ring 1.0s, silence 0.4s, Ring 1.0s, silence 0.8s
        is_ringing = (t < 1.0) or (1.4 <= t < 2.4)
        if is_ringing:
            # 20 Hz amplitude modulation for classic bell cadence
            ring_mod = 0.8 + 0.2 * math.sin(2.0 * math.pi * 20.0 * t)
            tone = (synth_sine(440.0, t) + synth_sine(480.0, t)) * 0.3 * ring_mod
            samples[f] = tone
        else:
            samples[f] = 0.0
            
    write_wav(out_path, samples, num_channels=1)

def generate_keyboard_typing(out_path):
    """Generate realistic mechanical keyboard typing burst (~1.2s)."""
    duration = 1.2
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_frames
    
    # 8 keystrokes at semi-random timings
    strokes = [0.05, 0.16, 0.28, 0.45, 0.58, 0.72, 0.88, 1.02]
    rng = random.Random(42)
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        val = 0.0
        for st in strokes:
            dt = t - st
            if 0 <= dt < 0.06:
                # Click: initial click pulse + plastic bottom-out resonance
                click_fq = 2800.0 + rng.uniform(-300, 300)
                click = synth_sine(click_fq, dt) * math.exp(-dt * 150.0) * 0.45
                thud = synth_sine(350.0, dt) * math.exp(-dt * 80.0) * 0.35
                noise = rng.uniform(-0.1, 0.1) * math.exp(-dt * 120.0)
                val += (click + thud + noise)
        samples[f] = max(-1.0, min(1.0, val))
        
    write_wav(out_path, samples, num_channels=1)

def generate_switch_toggle(out_path):
    """Heavy industrial toggle switch for defense switches (~0.35s)."""
    duration = 0.35
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_frames
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        # Initial mechanical release (0-0.03s), main click (0.04-0.12s), spring resonance
        val = 0.0
        if t < 0.03:
            val = synth_sine(400.0, t) * math.exp(-t * 100.0) * 0.3
        elif t >= 0.035:
            dt = t - 0.035
            snap = synth_sine(2200.0, dt) * math.exp(-dt * 90.0) * 0.6
            snap2 = synth_sine(4800.0, dt) * math.exp(-dt * 160.0) * 0.4
            body = synth_sine(180.0, dt) * math.exp(-dt * 45.0) * 0.5
            val = snap + snap2 + body
        samples[f] = max(-1.0, min(1.0, val))
        
    write_wav(out_path, samples, num_channels=1)

def generate_laser_scan(out_path):
    """Sci-fi laser scanning / phishing inspection beam (~1.4s)."""
    duration = 1.4
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_frames
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        # Frequency sweep up and down
        p = t / duration
        freq = 300.0 + 1800.0 * math.sin(math.pi * p)
        lfo = 1.0 + 0.15 * math.sin(2.0 * math.pi * 32.0 * t)
        env = adsr(t, duration, a=0.1, d=0.2, s=0.8, r=0.2)
        val = synth_pulse(freq * lfo, t, 0.3, 5) * 0.4 * env
        samples[f] = val
        
    write_wav(out_path, samples, num_channels=1)

def generate_teleport_whoosh(out_path):
    """VR teleport whoosh for smooth navigation (~0.55s)."""
    duration = 0.55
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_frames
    rng = random.Random(99)
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        p = t / duration
        env = math.sin(math.pi * p)
        
        # Sub-bass swoop (120Hz down to 50Hz)
        swoop_freq = 120.0 - 70.0 * p
        sub = synth_sine(swoop_freq, t) * 0.5 * env
        
        # Filtered airy noise
        noise = rng.uniform(-0.25, 0.25) * env
        
        samples[f] = (sub + noise) * 0.7
        
    write_wav(out_path, samples, num_channels=1)

def generate_room_ambience(out_path):
    """8s loopable Bank Office HVAC & Server Room Hum."""
    duration = 8.0
    total_frames = int(SAMPLE_RATE * duration)
    samples = [0.0] * (total_frames * 2)
    rng = random.Random(1234)
    
    b0 = b1 = b2 = b3 = b4 = b5 = b6 = 0.0
    
    for f in range(total_frames):
        t = f / SAMPLE_RATE
        
        # Pink noise for smooth air conditioning
        white = rng.uniform(-1.0, 1.0)
        b0 = 0.99886 * b0 + white * 0.0555179
        b1 = 0.99332 * b1 + white * 0.0750759
        b2 = 0.96900 * b2 + white * 0.1538520
        b3 = 0.86650 * b3 + white * 0.3104856
        b4 = 0.55000 * b4 + white * 0.5329522
        b5 = -0.7616 * b5 - white * 0.0168980
        pink = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362) * 0.02
        b6 = white * 0.115926
        
        # 60Hz and 120Hz transformer & server hum
        hum60 = synth_sine(60.0, t) * 0.06
        hum120 = synth_sine(120.0, t) * 0.04
        hum180 = synth_sine(180.0, t) * 0.02
        
        combined = pink + hum60 + hum120 + hum180
        samples[f * 2] = combined
        samples[f * 2 + 1] = combined
        
    write_wav(out_path, samples)

if __name__ == '__main__':
    base_dir = r"Assets/_AndesSim/Audio"
    
    print("Generating High-Quality Audio & Music Assets for ANDES-SIM...")
    # Music tracks (Stereo, 16-bit 44.1kHz)
    generate_menu_music(os.path.join(base_dir, "Music", "Music_VrStartMenu_Intro.wav"))
    generate_investigation_music(os.path.join(base_dir, "Music", "Music_CyberInvestigation_Theme.wav"))
    generate_attack_music(os.path.join(base_dir, "Music", "Music_AttackTension_Theme.wav"))
    generate_defense_music(os.path.join(base_dir, "Music", "Music_DefenseVictory_Theme.wav"))
    
    # SFX tracks (Mono, 16-bit 44.1kHz)
    generate_phone_ring(os.path.join(base_dir, "SFX", "SFX_Phone_Ring_3D.wav"))
    generate_keyboard_typing(os.path.join(base_dir, "SFX", "SFX_Keyboard_Typing.wav"))
    generate_switch_toggle(os.path.join(base_dir, "SFX", "SFX_Switch_Toggle.wav"))
    generate_laser_scan(os.path.join(base_dir, "SFX", "SFX_Cyber_LaserScan.wav"))
    generate_teleport_whoosh(os.path.join(base_dir, "SFX", "SFX_Teleport_Whoosh.wav"))
    
    # Ambience
    generate_room_ambience(os.path.join(base_dir, "Ambience", "SFX_Office_Atmosphere_Loop.wav"))
    print("ALL 10 AUDIO ASSETS GENERATED SUCCESSFULLY!")
