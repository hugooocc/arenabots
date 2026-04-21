using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BotAgent : Agent
{
    public Transform target;
    public float moveSpeed = 5f;
    public float shootCooldown = 1f;
    private float lastShootTime;
    private Rigidbody2D rb;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset positions
        transform.localPosition = Vector3.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        
        if (target != null)
        {
            target.GetComponent<TrainingTarget>()?.ResetPosition();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 8 floats: pos(2), targetPos(2), vel(2), cooldown(1), distance(1)
        sensor.AddObservation((Vector2)transform.localPosition);
        sensor.AddObservation((Vector2)target.localPosition);
        sensor.AddObservation(rb.linearVelocity);
        
        float cooldownStatus = Mathf.Clamp01((Time.time - lastShootTime) / shootCooldown);
        sensor.AddObservation(cooldownStatus);
        
        sensor.AddObservation(Vector2.Distance(transform.localPosition, target.localPosition));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Branch 0: Horizontal (0: None, 1: Left, 2: Right)
        float moveX = 0f;
        if (actions.DiscreteActions[0] == 1) moveX = -1f;
        else if (actions.DiscreteActions[0] == 2) moveX = 1f;

        // Branch 1: Vertical (0: None, 1: Down, 2: Up)
        float moveY = 0f;
        if (actions.DiscreteActions[1] == 1) moveY = -1f;
        else if (actions.DiscreteActions[1] == 2) moveY = 1f;

        rb.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

        // Branch 2: Shoot (0: None, 1: Shoot)
        if (actions.DiscreteActions[2] == 1)
        {
            Shoot();
        }

        // Small negative reward for step
        AddReward(-0.001f);
    }

    private void Shoot()
    {
        if (Time.time - lastShootTime >= shootCooldown)
        {
            lastShootTime = Time.time;
            // Logic to check if hit target (procedural aiming assumed)
            float angle = Vector2.Angle(transform.up, (target.localPosition - transform.localPosition).normalized);
            if (angle < 10f) // Simplified "hit" check for training
            {
                AddReward(1.0f); // Reward for hit
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        
        // Horizontal
        if (Input.GetKey(KeyCode.A)) discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.D)) discreteActions[0] = 2;
        else discreteActions[0] = 0;

        // Vertical
        if (Input.GetKey(KeyCode.S)) discreteActions[1] = 1;
        else if (Input.GetKey(KeyCode.W)) discreteActions[1] = 2;
        else discreteActions[1] = 0;

        // Shoot
        discreteActions[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
