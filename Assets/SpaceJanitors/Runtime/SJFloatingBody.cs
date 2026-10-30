using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SJFloatingBody : MonoBehaviour
{
    Rigidbody body;
    bool floating;
    void Awake(){body=GetComponent<Rigidbody>();body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;}
    void FixedUpdate(){
        var game=SJGame.Instance;if(!game || !game.player)return;
        var player=game.player;
        if(player.held && player.held.Body==body)return;
        bool next=player.zeroGravity || !player.InsideShip(body.worldCenterOfMass);
        body.useGravity=!next;body.linearDamping=next?.12f:.5f;body.angularDamping=next?.08f:.5f;
        if(next && !floating){
            // A small initial disturbance lifts objects off the deck; gravity alone cannot.
            body.AddForce(new Vector3(.08f,.65f,.06f),ForceMode.VelocityChange);
            body.AddTorque(new Vector3(.12f,.08f,.1f),ForceMode.VelocityChange);
        }
        floating=next;
    }
}
