using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Audio
{
    /// <summary>
    /// Gera sons provisórios (sintetizados) para testar as cenas enquanto não há áudio de verdade:
    /// motor do helicóptero, tiro, chiado de rádio, bipe da mira laser e portão abrindo. Grava WAVs em Art/Audio/Provisorio; para
    /// trocar por um som definitivo, basta substituir o arquivo mantendo o nome, ou apontar a
    /// fonte de som para outro arquivo.
    /// Menu: Nibiru > Áudio > Gerar sons provisórios.
    /// </summary>
    public static class ProvisionalSounds
    {
        public const string Folder = "Assets/_Nibiru/Art/Audio/Provisorio";
        public const string Helicopter = Folder + "/Helicoptero_Loop.wav";
        public const string Gunshot = Folder + "/Tiro.wav";
        public const string RadioStatic = Folder + "/Radio_Chiado_Loop.wav";
        public const string LaserWarning = Folder + "/Mira_Aviso.wav";
        public const string GateOpening = Folder + "/Portao_Abrindo.wav";

        const int Rate = 44100;

        [MenuItem("Nibiru/Áudio/Gerar sons provisórios")]
        static void GenerateFromMenu()
        {
            Generate(overwrite: true);
            Debug.Log("Sons provisórios gerados em " + Folder);
        }

        /// <summary>Gera os arquivos que faltam (ou todos, com overwrite) e devolve quando estão importados.</summary>
        public static void Generate(bool overwrite)
        {
            Directory.CreateDirectory(Folder);
            Write(Helicopter, overwrite, MakeHelicopter);
            Write(Gunshot, overwrite, MakeGunshot);
            Write(RadioStatic, overwrite, MakeRadioStatic);
            Write(LaserWarning, overwrite, MakeLaserWarning);
            Write(GateOpening, overwrite, MakeGateOpening);
            AssetDatabase.Refresh();
        }

        static void Write(string path, bool overwrite, Func<float[]> make)
        {
            if (!overwrite && File.Exists(path)) return;
            WriteWav(path, make());
        }

        // Batidas graves da hélice (12 por segundo) sobre um ronco filtrado e o assobio da turbina.
        // 2 segundos com número inteiro de batidas, para o loop não ter emenda.
        static float[] MakeHelicopter()
        {
            var random = new System.Random(7);
            var samples = new float[Rate * 2];
            const float beatRate = 12f;
            float rumble = 0f, thumpNoise = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float phase = t * beatRate % 1f;
                float envelope = Mathf.Exp(-phase * 9f);

                float white = (float)(random.NextDouble() * 2 - 1);
                thumpNoise += (white - thumpNoise) * 0.08f;
                rumble += (white - rumble) * 0.02f;

                float thump = (thumpNoise * 2.2f + Mathf.Sin(2 * Mathf.PI * 55f * t) * 0.5f) * envelope;
                float whine = Mathf.Sin(2 * Mathf.PI * 1180f * t) * 0.025f;
                samples[i] = thump * 0.8f + rumble * 1.5f + whine;
            }
            return Normalize(samples, 0.8f);
        }

        // Estalo curto, corpo grave e um eco longo, como um tiro ouvido em campo aberto.
        static float[] MakeGunshot()
        {
            var random = new System.Random(11);
            var samples = new float[(int)(Rate * 1.8f)];
            float body = 0f, tail = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                body += (white - body) * 0.15f;
                tail += (white - tail) * 0.04f;

                float crack = white * Mathf.Exp(-t / 0.004f);
                float boom = body * 3f * Mathf.Exp(-t / 0.09f) + Mathf.Sin(2 * Mathf.PI * 70f * t) * Mathf.Exp(-t / 0.12f) * 0.6f;
                float echoT = t - 0.18f;
                float echo = echoT > 0f ? tail * 4f * Mathf.Exp(-echoT / 0.5f) * Mathf.Clamp01(echoT / 0.05f) : 0f;
                samples[i] = crack + boom + echo * 0.5f;
            }
            return Normalize(samples, 0.95f);
        }

        // Chiado de rádio: ruído na faixa da voz, com volume que oscila e estalos de vez em quando.
        static float[] MakeRadioStatic()
        {
            var random = new System.Random(23);
            var samples = new float[Rate * 3];
            float low = 0f, lowest = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                low += (white - low) * 0.35f;
                lowest += (white - lowest) * 0.05f;
                float band = low - lowest;

                float sway = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * t / 3f) * Mathf.Sin(2 * Mathf.PI * 1.7f * t / 3f);
                float crackle = random.NextDouble() < 0.0004 ? white * 3f : 0f;
                samples[i] = band * sway + crackle;
            }
            // Suaviza o começo e o fim para o loop não estalar na emenda.
            int fade = Rate / 50;
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                samples[i] *= k;
                samples[samples.Length - 1 - i] *= k;
            }
            return Normalize(samples, 0.6f);
        }

        // Dois bipes agudos e secos, de aparelho: o laser do atirador travou na Elena.
        static float[] MakeLaserWarning()
        {
            var samples = new float[(int)(Rate * 0.45f)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float local = t < 0.25f ? t : t - 0.25f;
                bool on = local < 0.12f;
                float envelope = on ? Mathf.Clamp01(local / 0.005f) * Mathf.Clamp01((0.12f - local) / 0.01f) : 0f;
                float tone = Mathf.Sin(2 * Mathf.PI * 2350f * t) + Mathf.Sin(2 * Mathf.PI * 4700f * t) * 0.25f;
                samples[i] = tone * envelope;
            }
            return Normalize(samples, 0.5f);
        }

        // Portão de ferro correndo no trilho: motor grave, chiado de metal e uma batida no fim.
        static float[] MakeGateOpening()
        {
            var random = new System.Random(31);
            const float length = 4f, slam = 3.6f;
            var samples = new float[(int)(Rate * length)];
            float grind = 0f, low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                grind += (white - grind) * 0.3f;
                low += (white - low) * 0.03f;

                float moving = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((slam - t) / 0.2f);
                float motor = (Mathf.Sin(2 * Mathf.PI * 48f * t) * 0.5f + low * 2f) * moving;
                float squeal = Mathf.Sin(2 * Mathf.PI * (900f + 60f * Mathf.Sin(t * 7f)) * t) * 0.06f * moving;
                float rattle = grind * (0.25f + 0.2f * Mathf.Sin(t * 31f)) * moving;
                float hitT = t - slam;
                float hit = hitT > 0f ? (white * 1.5f + Mathf.Sin(2 * Mathf.PI * 90f * t)) * Mathf.Exp(-hitT / 0.08f) : 0f;
                samples[i] = motor + squeal + rattle + hit;
            }
            return Normalize(samples, 0.85f);
        }

        static float[] Normalize(float[] samples, float peak)
        {
            float max = 0.0001f;
            foreach (float s in samples) max = Mathf.Max(max, Mathf.Abs(s));
            for (int i = 0; i < samples.Length; i++) samples[i] = samples[i] / max * peak;
            return samples;
        }

        // WAV PCM 16 bits, mono.
        static void WriteWav(string path, float[] samples)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            int dataSize = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(Rate);
            writer.Write(Rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);
            foreach (float s in samples) writer.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
        }
    }
}
