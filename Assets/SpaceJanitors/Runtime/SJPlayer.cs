using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SJPlayer : MonoBehaviour
{
    public Camera view;
    public Transform leftHand, rightHand;
    public SJInteractable held;
    public bool tethered;
    public bool automated;
    public Vector3 tetherOrigin = new Vector3(14,1.3f,2.5f);
    public const float TetherLength = 18f;
    public float sensitivity = 1.8f;
    public float pitch;
    public bool zeroGravity = true;
    public bool Swimming => zeroGravity || InSpace;
    public Vector3 SwimVelocity => evaVelocity;
    float swimEffort;
    public bool useInteriorVolumes;
    public Bounds[] interiorVolumes;
    CharacterController cc;
    float verticalSpeed;
    Vector3 evaVelocity;
    LineRenderer rope;
    public bool InSpace => !InsideShip(transform.position + Vector3.up*.8f);
    public bool InsideShip(Vector3 point)
    {
        if(useInteriorVolumes) {foreach(var volume in interiorVolumes)if(volume.Contains(point))return true;return false;}
        return Mathf.Abs(point.x)<=12.65f && point.z>=-19.5f && point.z<=19.6f && point.y<=5.7f;
    }
    public CharacterController Controller => cc ? cc : (cc=GetComponent<CharacterController>());

    void Awake()
    {
        cc=GetComponent<CharacterController>();
        rope=gameObject.AddComponent<LineRenderer>();
        rope.material=new Material(Shader.Find("Sprites/Default"));
        rope.startColor=rope.endColor=new Color(1,.72f,.18f);
        rope.startWidth=rope.endWidth=.035f;
        rope.positionCount=12;rope.useWorldSpace=true;rope.enabled=false;
    }
    void Update()
    {
        if (automated || SJGame.Instance==null || SJGame.Instance.paused) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;
        sensitivity=SJProfile.Data.settings.sensitivity;
        transform.Rotate(0,Input.GetAxis("Mouse X")*sensitivity,0);
        pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*sensitivity*(SJProfile.Data.settings.invertY?-1:1),-82,82);
        view.transform.localRotation=Quaternion.Euler(pitch,0,0);
        var motion=new Vector2((Input.GetKey(SJProfile.Key(3))?1:0)-(Input.GetKey(SJProfile.Key(2))?1:0),(Input.GetKey(SJProfile.Key(0))?1:0)-(Input.GetKey(SJProfile.Key(1))?1:0));
        float lift=(Input.GetKey(SJProfile.Key(4))?1:0)-(Input.GetKey(SJProfile.Key(5))?1:0);
        MoveStep(motion,lift,Input.GetKey(SJProfile.Key(6)),Input.GetKeyDown(SJProfile.Key(4)),Time.deltaTime);
        var target=Target();
        if(Input.GetMouseButtonDown(0)) TryGrab(target);
        if(Input.GetMouseButtonUp(0)) Release();
        if(Input.GetKeyDown(SJProfile.Key(8))) Push(held ? held : target);
        if(Input.GetKey(SJProfile.Key(9)) && target && target.kind==SJInteractable.Kind.Repair) target.RepairFor(Time.deltaTime);
        if(Input.GetKeyDown(SJProfile.Key(9)) && target) {
            if(target.kind==SJInteractable.Kind.Tether) {tethered=!tethered;SJGame.Instance.Notify(tethered?"안전줄 연결 / TETHER ATTACHED":"안전줄 해제 / TETHER RELEASED");}
            if(target.kind==SJInteractable.Kind.Computer) SJGame.Instance.Notify(SJGame.Instance.Finished?"테스트 임무 성공! 자유롭게 더 둘러보세요.":"폐기물 5개 수거 + 수리 패널 2개 복구");
        }
        if(Input.GetKeyDown(SJProfile.Key(11))) Respawn();
        if(Input.GetKeyDown(SJProfile.Key(12))) {zeroGravity=!zeroGravity;SJGame.Instance.Notify(zeroGravity?"무중력 / 수영 이동":"실내 인공중력 복구");}
        if(Input.GetKey(SJProfile.Key(7))) Brake(Time.deltaTime);
        if(Input.GetKeyDown(SJProfile.Key(10))) {var lamp=view.GetComponentInChildren<Light>();lamp.enabled=!lamp.enabled;}
        if(Input.GetKeyDown(KeyCode.F2)) Teleport(new Vector3(10,0,0),Quaternion.Euler(0,90,0));
        if(Input.GetKeyDown(KeyCode.F3)) {tethered=true;Teleport(new Vector3(15.5f,1,0),Quaternion.Euler(0,0,0));}
    }
    public void MoveStep(Vector2 axes,float lift,bool fast,bool jump,float dt)
    {
        axes=Vector2.ClampMagnitude(axes,1);
        swimEffort=axes.magnitude+Mathf.Abs(lift);
        if(Swimming) {
            Vector3 wish=view.transform.right*axes.x+view.transform.forward*axes.y+Vector3.up*lift;
            // Gentle drag gives a swimming feel while preserving a short coast.
            evaVelocity+=Vector3.ClampMagnitude(wish,1)*(fast?8f:5f)*dt;
            evaVelocity*=Mathf.Exp(-1.25f*dt);
            evaVelocity=Vector3.ClampMagnitude(evaVelocity,fast?6f:3.8f);
            Vector3 step=evaVelocity*dt;
            if(tethered){Vector3 next=transform.position+step;Vector3 offset=next-tetherOrigin;
                if(offset.magnitude>TetherLength){step=tetherOrigin+offset.normalized*TetherLength-transform.position;evaVelocity=Vector3.ProjectOnPlane(evaVelocity,offset.normalized);}}
            Controller.Move(step);verticalSpeed=0;
        } else {
            evaVelocity=Vector3.zero;
            if(Controller.isGrounded && verticalSpeed<0)verticalSpeed=-2;
            if(jump && Controller.isGrounded)verticalSpeed=5.2f;
            verticalSpeed-=14*dt;
            Vector3 move=(transform.right*axes.x+transform.forward*axes.y)*(fast?5.3f:3.3f);
            move.y=verticalSpeed;Controller.Move(move*dt);
        }
        if(transform.position.y < -22 || transform.position.magnitude>85) Respawn();
    }
    public SJInteractable Target()
    {
        if(Physics.Raycast(view.transform.position,view.transform.forward,out RaycastHit hit,3.6f,~(1<<2),QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<SJInteractable>();
        return null;
    }
    public bool TryGrab(SJInteractable item)
    {
        if(!item || !item.CanGrab || !item.Body)return false;
        if(Vector3.Distance(view.transform.position,item.Body.worldCenterOfMass)>4.2f)return false;
        Release();held=item;held.Body.useGravity=false;
        held.Body.angularDamping=7;
        foreach(var col in held.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(Controller,col,true);
        return true;
    }
    public void Release()
    {
        if(!held)return;
        var previous=held;held=null;
        previous.Body.useGravity=!zeroGravity && InsideShip(previous.transform.position);
        previous.Body.angularDamping=.5f;
        foreach(var col in previous.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(Controller,col,false);
    }
    public bool Push(SJInteractable item)
    {
        if(!item || !item.Body || item.completed)return false;
        if(held==item)Release();
        item.Body.AddForce(view.transform.forward*7,ForceMode.VelocityChange);
        return true;
    }
    void FixedUpdate()
    {
        if(!held || SJGame.Instance.paused)return;
        Vector3 desired=view.transform.position+view.transform.forward*(held.kind==SJInteractable.Kind.Cargo?2.5f:2.0f);
        Vector3 difference=desired-held.Body.worldCenterOfMass;
        if(difference.magnitude>4.5f){Release();return;}
        held.Body.linearVelocity=Vector3.ClampMagnitude(difference*9,9);
    }
    void LateUpdate()
    {
        if(leftHand)leftHand.localPosition=Vector3.Lerp(leftHand.localPosition,held?new Vector3(-.26f,-.19f,.57f):new Vector3(-.31f,-.3f,.43f),Time.unscaledDeltaTime*9);
        if(rightHand)rightHand.localPosition=Vector3.Lerp(rightHand.localPosition,held?new Vector3(.26f,-.19f,.57f):new Vector3(.31f,-.3f,.43f),Time.unscaledDeltaTime*9);
        if(Swimming && !held && swimEffort>.01f && !SJGame.Instance.paused){
            float stroke=Mathf.Sin(Time.time*5f)*.065f;
            if(leftHand)leftHand.localPosition+=new Vector3(0,stroke,stroke)*Time.deltaTime*9;
            if(rightHand)rightHand.localPosition+=new Vector3(0,-stroke,-stroke)*Time.deltaTime*9;
        }
        if(!rope)return;rope.enabled=tethered;
        if(tethered)for(int i=0;i<12;i++){float t=i/11f;rope.SetPosition(i,Vector3.Lerp(tetherOrigin,transform.position+Vector3.up*.75f,t)+Vector3.down*Mathf.Sin(t*Mathf.PI)*.4f);}
    }
    public void Teleport(Vector3 pos,Quaternion rotation)
    {
        Release();Controller.enabled=false;transform.SetPositionAndRotation(pos,rotation);Controller.enabled=true;
        pitch=0;view.transform.localRotation=Quaternion.identity;verticalSpeed=0;evaVelocity=Vector3.zero;
        Physics.SyncTransforms();
    }
    public void Respawn(){tethered=false;Teleport(new Vector3(0,.1f,10),Quaternion.Euler(0,180,0));SJGame.Instance.Notify("중앙 복도로 복귀했습니다. / RETURNED TO SHIP");}
    public void Brake(float dt){evaVelocity*=Mathf.Exp(-9f*dt);}
    void OnControllerColliderHit(ControllerColliderHit hit){
        if(Swimming && Vector3.Dot(evaVelocity,hit.normal)<0)evaVelocity=Vector3.ProjectOnPlane(evaVelocity,hit.normal);
        var body=hit.collider.attachedRigidbody;
        if(body && !body.isKinematic && (!held || body!=held.Body))body.AddForceAtPosition(hit.moveDirection*1.5f,hit.point,ForceMode.Acceleration);
    }
    void OnApplicationFocus(bool focus){if(!focus && SJGame.Instance && !automated)SJGame.Instance.SetPaused(true);}
}
