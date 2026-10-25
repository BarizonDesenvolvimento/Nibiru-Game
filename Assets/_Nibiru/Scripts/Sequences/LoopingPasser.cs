using System.Collections;
using UnityEngine;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Figurante de ambiente que passa de vez em quando: a cada intervalo sorteado liga o objeto,
    /// percorre o caminho (<see cref="MoveAlongPath"/>) e desliga de novo. Serve para faróis e silhuetas
    /// que aparecem e somem dentro da tempestade. Não tem inteligência nem colisão.
    /// </summary>
    public class LoopingPasser : MonoBehaviour
    {
        [Tooltip("Quem anda. Fica desligado entre uma passada e outra.")]
        [SerializeField] MoveAlongPath mover;

        [SerializeField, Min(0f)] float minSecondsBetween = 12f;
        [SerializeField, Min(0f)] float maxSecondsBetween = 25f;

        [Tooltip("Espera antes da primeira passada.")]
        [SerializeField, Min(0f)] float firstDelay = 6f;

        void OnEnable()
        {
            mover.gameObject.SetActive(false);
            StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            yield return new WaitForSeconds(firstDelay);
            while (true)
            {
                mover.gameObject.SetActive(true);
                mover.Play();
                while (mover.IsMoving) yield return null;
                mover.gameObject.SetActive(false);
                yield return new WaitForSeconds(Random.Range(minSecondsBetween, maxSecondsBetween));
            }
        }
    }
}
