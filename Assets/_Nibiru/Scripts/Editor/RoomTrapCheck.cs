using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Procura cantos que prendem a Elena numa sala, na malha de navegação da cena aberta. Para cada
    /// ponto andável e cada uma de 8 direções, põe um inimigo grande (o Lullu, ou o Primeiro Servo)
    /// colado nela, na direção de onde ele vem, e vê se ela ainda alcança um ponto livre longe dele
    /// andando só por onde ele não alcança. Os pontos em que alguma direção não tem saída aparecem num
    /// mapa de texto no Console (número = em quantas das 8 direções ela fica presa, 1 a 8).
    /// Serve para o Poço (antecâmara) e para outras arenas: mude os limites abaixo.
    /// Menu: Nibiru > Testes > Conferir cantos sem saída (Poço).
    /// </summary>
    static class RoomTrapCheck
    {
        // Limites da área conferida (o fundo do Poço e a antecâmara) e a faixa de altura.
        static readonly Vector2 MinXZ = new Vector2(35f, 70f);
        static readonly Vector2 MaxXZ = new Vector2(55f, 103f);
        const float MinY = -14.5f, MaxY = -9.5f;

        const float Cell = 0.5f;
        const float PlayerRadius = 0.05f;      // a malha já fica a um raio do agente das paredes
        const float ThreatDistance = 1.4f;     // onde ele está, em relação a ela
        const float ThreatReach = 1.1f;        // raio que ele bloqueia (o Servo, o maior, já cabe aqui)
        const float FreeDistance = 5f;         // a que distância dele ela está a salvo
        const float MaxStep = 0.7f;

        [MenuItem("Nibiru/Testes/Conferir cantos sem saída (Poço)")]
        static void Run()
        {
            var cells = new List<Vector3>();
            var index = new Dictionary<(int, int, int), int>();
            var columns = new Dictionary<(int, int), List<int>>();
            for (float x = MinXZ.x + Cell / 2; x < MaxXZ.x; x += Cell)
            for (float z = MinXZ.y + Cell / 2; z < MaxXZ.y; z += Cell)
            for (float y = MinY; y <= MaxY; y += 0.25f)
            {
                if (!NavMesh.SamplePosition(new Vector3(x, y, z), out NavMeshHit hit, 0.2f, NavMesh.AllAreas)) continue;
                if (Mathf.Abs(hit.position.x - x) > 0.12f || Mathf.Abs(hit.position.z - z) > 0.12f) continue;
                if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, NavMesh.AllAreas) && edge.distance < PlayerRadius) continue;
                var key = (Mathf.FloorToInt(x / Cell), Mathf.FloorToInt(z / Cell), Mathf.RoundToInt(hit.position.y * 4f));
                var column = (key.Item1, key.Item2);
                // Mesma coluna e quase a mesma altura = o mesmo piso amostrado de duas alturas.
                if (columns.TryGetValue(column, out var existing) && existing.Exists(e => Mathf.Abs(cells[e].y - hit.position.y) < 0.4f)) continue;
                index[key] = cells.Count;
                if (!columns.TryGetValue(column, out var list)) columns[column] = list = new List<int>();
                list.Add(cells.Count);
                cells.Add(hit.position);
            }

            // Vizinhos: 8 direções, degrau pequeno e caminho livre na malha.
            var neighbors = new List<int>[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                neighbors[i] = new List<int>();
                int cx = Mathf.FloorToInt(cells[i].x / Cell), cz = Mathf.FloorToInt(cells[i].z / Cell);
                for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    if (!columns.TryGetValue((cx + dx, cz + dz), out var near)) continue;
                    foreach (var kv in near)
                    {
                        if (Mathf.Abs(cells[kv].y - cells[i].y) > MaxStep) continue;
                        if (!NavMesh.Raycast(cells[i], cells[kv], out _, NavMesh.AllAreas)) neighbors[i].Add(kv);
                    }
                }
            }

            // Só conta o que se alcança andando do fundo do Poço (a malha tem ilhas, como o piso sob as plataformas).
            var reachable = new bool[cells.Count];
            {
                int start = 0; float best = float.MaxValue;
                for (int i = 0; i < cells.Count; i++)
                {
                    float d = (cells[i] - new Vector3(45f, -13.92f, 77.25f)).sqrMagnitude;
                    if (d < best) { best = d; start = i; }
                }
                var open = new Queue<int>();
                open.Enqueue(start);
                reachable[start] = true;
                while (open.Count > 0)
                    foreach (int n in neighbors[open.Dequeue()])
                        if (!reachable[n]) { reachable[n] = true; open.Enqueue(n); }
            }

            var trapped = new int[cells.Count];
            var visited = new int[cells.Count];
            int stamp = 0;
            var queue = new Queue<int>();
            for (int i = 0; i < cells.Count; i++)
            for (int d = 0; d < 8; d++)
            {
                if (!reachable[i]) continue;
                float angle = d * 45f * Mathf.Deg2Rad;
                Vector3 threat = cells[i] + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ThreatDistance;
                stamp++;
                queue.Clear();
                queue.Enqueue(i);
                visited[i] = stamp;
                bool free = false;
                while (queue.Count > 0 && !free)
                {
                    int c = queue.Dequeue();
                    if (Flat(cells[c] - threat).magnitude >= FreeDistance) { free = true; break; }
                    foreach (int n in neighbors[c])
                    {
                        if (visited[n] == stamp || Flat(cells[n] - threat).magnitude < ThreatReach) continue;
                        visited[n] = stamp;
                        queue.Enqueue(n);
                    }
                }
                if (!free) trapped[i]++;
            }

            // Mapa de texto: uma linha por z (do norte para o sul), pior caso entre as alturas.
            var worst = new Dictionary<(int, int), int>();
            int trappedCells = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (!reachable[i]) continue;
                var k = (Mathf.FloorToInt(cells[i].x / Cell), Mathf.FloorToInt(cells[i].z / Cell));
                worst[k] = Mathf.Max(worst.TryGetValue(k, out int w) ? w : 0, trapped[i]);
                if (trapped[i] > 0) trappedCells++;
            }
            var map = new StringBuilder();
            for (float z = MaxXZ.y - Cell / 2; z > MinXZ.y; z -= Cell)
            {
                map.Append($"z{z,6:0.0} ");
                for (float x = MinXZ.x + Cell / 2; x < MaxXZ.x; x += Cell)
                {
                    var k = (Mathf.FloorToInt(x / Cell), Mathf.FloorToInt(z / Cell));
                    map.Append(!worst.TryGetValue(k, out int v) ? '#' : v == 0 ? '.' : (char)('0' + Mathf.Min(v, 9)));
                }
                map.AppendLine();
            }
            Debug.Log($"Cantos sem saída: {trappedCells} de {cells.Count} pontos presos em alguma direção (inimigo a {ThreatDistance} m, " +
                      $"alcance {ThreatReach} m, a salvo a {FreeDistance} m). x de {MinXZ.x} a {MaxXZ.x}, uma coluna a cada {Cell} m; " +
                      $"# = parede, . = livre, número = direções sem saída.\n{map}");
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
