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

        public const string LulluBreathing = Folder + "/Lullu_Respiracao_Loop.wav";
        public const string WellAmbience = Folder + "/Poco_Ambiente_Loop.wav";
        public const string ServantRoar = Folder + "/Servo_Urro.wav";
        public const string MaskCrack = Folder + "/Mascara_Racha.wav";
        public const string FloodlightCrash = Folder + "/Holofote_Queda.wav";
        public const string WallOpening = Folder + "/Parede_Abrindo.wav";
        public const string TunnelAmbience = Folder + "/Passagem_Ambiente_Loop.wav";

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
            Write(LulluBreathing, overwrite, MakeLulluBreathing);
            Write(WellAmbience, overwrite, MakeWellAmbience);
            Write(ServantRoar, overwrite, MakeServantRoar);
            Write(MaskCrack, overwrite, MakeMaskCrack);
            Write(FloodlightCrash, overwrite, MakeFloodlightCrash);
            Write(WallOpening, overwrite, MakeWallOpening);
            Write(TunnelAmbience, overwrite, MakeTunnelAmbience);
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

        // Respiração rouca que não é de gente (duas inspirações lentas em 5 s) e unhas raspando pedra.
        static float[] MakeLulluBreathing()
        {
            var random = new System.Random(41);
            var samples = new float[Rate * 5];
            float low = 0f, lowest = 0f, scrape = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                low += (white - low) * 0.12f;
                lowest += (white - lowest) * 0.015f;
                float breath = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2 * Mathf.PI * t / 2.5f)), 1.5f);
                float rasp = (low - lowest) * breath * (0.7f + 0.3f * Mathf.Sin(t * 53f));

                // Raspão: ruído áspero em rajadas curtas, uma vez e meia por segundo.
                float gate = Mathf.Max(0f, Mathf.Sin(2 * Mathf.PI * t * 0.6f + 1f)) > 0.85f ? 1f : 0f;
                scrape += (white - scrape) * 0.45f;
                samples[i] = rasp * 1.6f + scrape * gate * 0.35f * (0.5f + 0.5f * Mathf.Sin(t * 90f));
            }
            return FadeEdges(Normalize(samples, 0.7f));
        }

        // Fundo do Poço: ronco grave e ar parado, com duas pedras que caem e batem ao longe.
        static float[] MakeWellAmbience()
        {
            var random = new System.Random(53);
            var samples = new float[Rate * 8];
            float rumble = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                rumble += (white - rumble) * 0.004f;
                float drone = Mathf.Sin(2 * Mathf.PI * 38f * t) * 0.18f + Mathf.Sin(2 * Mathf.PI * 57f * t) * 0.08f;
                float stones = 0f;
                foreach (float at in new[] { 1.7f, 2.0f, 5.6f })
                {
                    float hit = t - at;
                    if (hit < 0f) continue;
                    stones += (white * 0.9f + Mathf.Sin(2 * Mathf.PI * (260f + at * 40f) * t) * 0.5f) * Mathf.Exp(-hit / 0.05f);
                }
                samples[i] = rumble * 14f + drone + stones * 0.5f;
            }
            return FadeEdges(Normalize(samples, 0.6f));
        }

        // Urro do Primeiro Servo: um rugido grave e rouco que sobe e cai, com a garganta raspando.
        static float[] MakeServantRoar()
        {
            var random = new System.Random(61);
            var samples = new float[(int)(Rate * 1.9f)];
            float low = 0f, lowest = 0f, phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                low += (white - low) * 0.2f;
                lowest += (white - lowest) * 0.02f;
                float envelope = Mathf.Clamp01(t / 0.15f) * Mathf.Exp(-Mathf.Max(0f, t - 1.1f) / 0.3f);
                // Altura da voz: sobe no começo e afunda no fim.
                float pitch = 70f + 40f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.6f));
                phase += pitch / Rate;
                float voice = Mathf.Sin(2 * Mathf.PI * phase) + 0.5f * Mathf.Sin(4 * Mathf.PI * phase) + 0.3f * Mathf.Sin(6 * Mathf.PI * phase);
                float growl = (low - lowest) * (0.6f + 0.4f * Mathf.Sin(t * 2 * Mathf.PI * 28f));
                samples[i] = (voice * 0.6f * (0.7f + 0.3f * Mathf.Sin(t * 2 * Mathf.PI * 9f)) + growl * 2.2f) * envelope;
            }
            return Normalize(samples, 0.95f);
        }

        // A máscara rachando: estalos secos de metal e pedra, um atrás do outro.
        static float[] MakeMaskCrack()
        {
            var random = new System.Random(67);
            var samples = new float[(int)(Rate * 0.7f)];
            float body = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                body += (white - body) * 0.5f;
                float s = 0f;
                foreach (float at in new[] { 0f, 0.07f, 0.16f, 0.31f })
                {
                    float hit = t - at;
                    if (hit < 0f) continue;
                    s += (body * 1.4f + Mathf.Sin(2 * Mathf.PI * (2400f - at * 3000f) * t) * 0.4f) * Mathf.Exp(-hit / 0.012f);
                }
                samples[i] = s;
            }
            return Normalize(samples, 0.8f);
        }

        // Holofote caindo: o poste bate de metal no chão e o vidro se quebra.
        static float[] MakeFloodlightCrash()
        {
            var random = new System.Random(71);
            var samples = new float[(int)(Rate * 1.4f)];
            float body = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                body += (white - body) * 0.1f;
                float clang = (Mathf.Sin(2 * Mathf.PI * 310f * t) + Mathf.Sin(2 * Mathf.PI * 467f * t) * 0.7f + Mathf.Sin(2 * Mathf.PI * 1230f * t) * 0.3f) * Mathf.Exp(-t / 0.35f);
                float thud = body * 4f * Mathf.Exp(-t / 0.08f);
                float glassT = t - 0.04f;
                float glass = glassT > 0f && random.NextDouble() < 0.08 ? white * 2.5f * Mathf.Exp(-glassT / 0.25f) : 0f;
                samples[i] = clang * 0.5f + thud + glass;
            }
            return Normalize(samples, 0.95f);
        }

        // Parede de pedra afundando no chão: ronco grave, raspar de pedra contra pedra e uma batida no fim.
        static float[] MakeWallOpening()
        {
            var random = new System.Random(83);
            const float length = 5f, slam = 4.5f;
            var samples = new float[(int)(Rate * length)];
            float rumble = 0f, grind = 0f, grindLow = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                rumble += (white - rumble) * 0.012f;
                grind += (white - grind) * 0.25f;
                grindLow += (white - grindLow) * 0.03f;
                float envelope = Mathf.Clamp01(t / 0.4f) * Mathf.Clamp01((slam - t) / 0.3f + 0.2f);
                float scrape = (grind - grindLow) * (0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 6f * t));
                float s = (rumble * 6f + scrape * 0.9f + Mathf.Sin(2 * Mathf.PI * 38f * t) * 0.25f) * (t < slam ? envelope : 0f);
                float hit = t - slam;
                if (hit >= 0f) s += (rumble * 10f + Mathf.Sin(2 * Mathf.PI * 46f * t) * 1.4f) * Mathf.Exp(-hit / 0.18f);
                samples[i] = s;
            }
            return Normalize(samples, 0.9f);
        }

        // Túnel antigo: vento baixo que sobe e desce e, de vez em quando, um pingo. 4 s sem emenda.
        static float[] MakeTunnelAmbience()
        {
            var random = new System.Random(89);
            var samples = new float[Rate * 4];
            float wind = 0f, windLow = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)(random.NextDouble() * 2 - 1);
                wind += (white - wind) * 0.02f;
                windLow += (white - windLow) * 0.004f;
                float sway = 0.55f + 0.45f * Mathf.Sin(2 * Mathf.PI * t / 4f);
                float s = (wind - windLow) * 5f * sway;
                foreach (float at in new[] { 1.3f, 3.05f })
                {
                    float drop = t - at;
                    if (drop >= 0f) s += Mathf.Sin(2 * Mathf.PI * (1900f - drop * 2500f) * drop) * Mathf.Exp(-drop / 0.04f) * 0.35f;
                }
                samples[i] = s;
            }
            return Normalize(FadeEdges(samples), 0.6f);
        }

        // Suaviza o começo e o fim para o loop não estalar na emenda.
        static float[] FadeEdges(float[] samples)
        {
            int fade = Rate / 20;
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                samples[i] *= k;
                samples[samples.Length - 1 - i] *= k;
            }
            return samples;
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
