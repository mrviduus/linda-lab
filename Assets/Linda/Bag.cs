using UnityEngine;

// Punching bag on a chain: a pendulum. The whip kicks it away; on the way back it hits Linda in the back and she
// takes hit_back. Whipping a bag that is already flying at her ricochets: she is the one knocked down.
public class Bag : MonoBehaviour
{
    public Vector3 anchor = new Vector3(1.5f, 3f, 0.3f);
    public float length = 2.2f, gravity = 9.8f, damping = 0.4f;
    public float whipKick = 4f;     // angular speed the whip adds
    public float whipReach = 2.8f;  // units in front of Linda the whip covers
    public float hitSpeed = 1.5f;   // bag speed (units/s) toward Linda that knocks her down
    public float hitRange = 0.45f;

    float theta, omega;
    bool kicked;                    // this whip swing already hit
    Transform chain, sack;
    LindaController linda;

    void Start()
    {
        chain = transform.Find("Chain"); sack = transform.Find("Sack");
        linda = FindFirstObjectByType<LindaController>();
    }

    void OnGUI() { GUI.Label(new Rect(8, 28, 400, 24), string.Format("bag theta {0:F2} omega {1:F2}", theta, omega)); }

    void Update()
    {
        var dt = Time.deltaTime;
        omega += (-gravity / length * Mathf.Sin(theta) - damping * omega) * dt;
        theta += omega * dt;
        var pos = anchor + length * new Vector3(Mathf.Sin(theta), -Mathf.Cos(theta), 0);
        sack.position = pos;
        chain.position = (anchor + pos) / 2 + Vector3.forward * 0.01f;   // behind the sack
        chain.rotation = Quaternion.Euler(0, 0, -theta * Mathf.Rad2Deg);
        chain.localScale = new Vector3(0.04f, length, 1);

        var dx = pos.x - linda.transform.position.x;           // bag relative to Linda
        var toward = Mathf.Sign(-dx);                          // direction the bag must move to reach her
        var speed = omega * length * Mathf.Cos(theta);         // horizontal bag speed, +x
        var flying = speed * toward > hitSpeed;

        if (linda.WhipOut && !kicked && dx * linda.Facing > 0.3f && Mathf.Abs(dx) < whipReach)
        {
            kicked = true;
            if (flying) linda.Knock(toward);                  // ricochet: the bag wins
            else omega += whipKick * linda.Facing / length;
        }
        if (!linda.WhipOut) kicked = false;
        if (flying && Mathf.Abs(dx) < hitRange && linda.Free) linda.Knock(toward);
    }
}
