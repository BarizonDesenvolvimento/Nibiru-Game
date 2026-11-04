using UnityEngine;

namespace Nibiru.EditorTools.Content
{
    /// <summary>
    /// O círculo da equipe em volta da fogueira do Acampamento (passo 2), numa tabela só: o Blockout põe as caixas
    /// dos corpos, o CampContent põe as áreas de "Examinar" e a bolsa do Tom, o TeamBodiesSetup põe os modelos, e os
    /// testes conferem por aqui.
    ///
    /// Decisão do dono (3/nov/2026): 4 lugares a 90° entre si, 3 com corpos (um de cada modelo) e o 4º vazio, o do Tom,
    /// só com a bolsa da câmera no chão. O círculo é girado para a barraca da Elena (ela sai na direção de 190°): o
    /// corpo 1 fica de frente para ela, e o lugar vazio, ao sul (o lado da barraca do Tom), aparece logo à direita.
    /// Ângulos em graus, 0 = leste, contando no sentido anti-horário visto de cima.
    /// </summary>
    public static class CampCircle
    {
        public const float GroundY = 3f;
        public static readonly Vector3 Center = new Vector3(0f, GroundY, -112f);
        public const float Radius = 3.2f;

        /// <summary>Ângulo do lugar de cada corpo (Corpo_1, Corpo_2, Corpo_3).</summary>
        public static readonly float[] BodyAngles = { 190f, 10f, 100f };

        /// <summary>O lugar vazio do Tom e a bolsa da câmera, um pouco para dentro e de lado, largada no chão.</summary>
        public const float TomAngle = 280f;
        public const float BagRadius = 3.0f;
        public const float BagYaw = 35f;

        public static int BodyCount => BodyAngles.Length;

        /// <summary>Ponto no chão, no ângulo e na distância do centro dados.</summary>
        public static Vector3 Place(float angle, float radius = Radius)
        {
            float a = angle * Mathf.Deg2Rad;
            return new Vector3(Center.x + Mathf.Cos(a) * radius, GroundY, Center.z + Mathf.Sin(a) * radius);
        }

        /// <summary>Lugar do corpo N (de 1 a 3), no chão.</summary>
        public static Vector3 BodyPlace(int n) => Place(BodyAngles[n - 1]);

        /// <summary>Giro do lugar do corpo N: o +z local aponta para fora do círculo.</summary>
        public static float BodyYaw(int n) => 90f - BodyAngles[n - 1];

        public static Vector3 TomPlace => Place(TomAngle);
        public static Vector3 BagPosition => Place(TomAngle, BagRadius);
    }
}
