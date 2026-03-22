

using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;

// Rigidbody2D 컴포넌트를 담을 변수를 선언
RigidBody2D rigid;
public float maxSpeed;
public float jumpPower;
SpriteRenderer spriteRenderer;
Animator anim;

void Awake()
{
    // 스크립트가 실행될 때 해당 게임 오브젝트에 붙어있는 Rigidbody2D 컴포넌트를 가져와 변수에 할당합니다.
    // (초기화 단계에서 미리 찾아두면 성능상 유리합니다.)
    rigid = GetComponent<RigidBody2D>();

    //내 캐릭터의 '몸(이미지)'을 조절할 수 있는 리모컨을 가져오는 것
    /*가지고 오는 이유는 
    좌우 반전 (flipX): 캐릭터가 왼쪽을 볼 때 이미지를 휙 돌리기

    색깔 변경 (color): 대미지를 입었을 때 캐릭터를 빨갛게 만들기

    이미지 교체 (sprite): 아이템을 먹었을 때 캐릭터 옷을 바꾸기
    */
    spriteRenderer = GetComponent<SpriteRenderer>();

    //Animator는 그 이미지들을 갈아끼우며 **걷고, 뛰고, 점프하는 '동작'을 관리
    anim = GetComponent<Animator>();



}


void Update()
{
    if(Input.GetButtonDown("Jump") && !anim.GetBool("isJumping")) //점프 버튼을 눌렀고(&&), 현재 점프 중이 아닐 때(!isJumping)만 점프해라
    {
        //ForceMode2D.Impulse (폭발적인 힘)
        //"서서히 공중으로 떠오르는 것"이 아니라 바닥을 박차고 순간적으로 튀어 올라야 하기 때문에, 반드시 Impulse 모드를 쓰는게 좋다 
        rigid.AddForce(Vector2.up * jumpPower , ForceMode2D.Impulse);

        // "isJumping"이라는 이름의 애니메이터 파라미터(bool)를 true(참)로 바꿉니다.
        // 이 코드가 실행되는 순간, 애니메이터는 '점프 시작' 혹은 '공중 동작' 애니메이션으로 상태를 전환합니다.
        anim.SetBool("isJumping",true);
    }
    

    //플레이어가 이동 버튼에서 손을 뗐을 때, 캐릭터가 빙판 위를 걷는 것처럼 끝없이 미끄러지는 것을 방지하고 속도를 급격히 줄여서 멈추게 하려는 용도
    if (Input.GetButtonUp("Horizontal")) // 좌,우 버튼을 땠을때 
    {
        // normalized : 현재 캐릭터가 어느 방향으로 움직이고 있는지 그 **'방향성'**을 추출 , 벡터 크기를 1로 만든 상태 (단위 벡터), 방향구할때 사용
        /*ex)
        normalized는 벡터의 길이를 무조건 1로 압축해버립니다. (이를 '단위 벡터'라고 하죠.)

        오른쪽으로 가고 있었다면?: (5, 0) -> (1, 0) (오른쪽 방향만 남음)

        왼쪽으로 가고 있었다면?: (-5, 0) -> (-1, 0) (왼쪽 방향만 남음)

        가만히 있었다면?: (0, 0) -> (0, 0)

        즉, rigid.velocity.normalized를 쓰면 **"지금 어느 쪽으로 가고 있었는지"**라는 방향 정보만 쏙 빼오는 것
        */
        rigid.velocity = new Vecotr2(rigid.velocity.normalized * 0.5f, rigid.velocity.y);
    }

    // 방향 전환 
    if (Input.GetButtonDown("Horizontal")) 
    //왼쪽 키를 눌렀으면 이미지를 뒤집어라
    //좌우 반전 (flipX): 캐릭터가 왼쪽을 볼 때 이미지를 휙 돌리기
    //이코드는 좌,우 두개다 적용 된다 false일떄는 이미지 원상태 true일경우는 이미지 반전 
    spriteRenderer.flipX = Input.GetAxisRaw("Horizontal") == -1; //-1(왼쪽) 과 같으면 true가 되어서 이미지 반전 

    //속도(움직임)'를 감시해서 애니메이션을 걷기 상태로 바꿀지, 가만히 서 있는 상태로 바꿀지 결정하는 '자동 스위치
    //velocity.x는 좌우 속도 
    // MathF.Abs 절대값을 적용 시킨다 왜 적용 시키냐면 왼쪽은 -값이어서 0.3보다 작기 때문에 워킹 상태가 유지된다 그러므로 절대값을 적용시켜서 음수값을 제거
    if(MathF.Abs(rigid.velocity.x)< 0.3) //지금 좌우로 전혀 움직이지 않고 멈춰있는 상태인가?
    anim.SetBool("isWalking",false); //캐릭터가 '기다리기(Idle)' 모션
    else
    anim.SetBool("isWalking",true); //'걷기(Walk)' 모션을 재생

}

//일반 Update에서 물리 힘을 주면 컴퓨터 사양(프레임)에 따라 이동 속도가 들쑥날쑥해질 수 있으므로 물리현상은 FixedUpdate에서 처리하는게좋다 
//FixedUpdate는 고정된 시간 간격으로 실행되어 안정적
void FixedUpdate()
{
    // Horizontal = 좌우 방향 입력
    // 키보드 가로 입력(-1.0, 0, 1.0)을 정수 단위로 즉시 가져옵니다. 
    // GetAxisRaw는 부드러운 가속 없이 즉각적인 반응을 줄 때 사용합니다.
    float h = Input.GetAxisRaw("Horizontal");
    
    // 물체에 오른쪽 방향(Vector2.right)으로 입력값(h)만큼의 힘을 가합니다.
    // vector.right에다가 h를 곱하는 이유는 좌(-1.0) 우(1,0) 을가지고 와서 곱해서 좌우전환을 할수 있게 하기위해서다 
    // Vector2.left를 안쓰는 이유는 Vector2.right * h를 쓰는게 더 좌우 처리가 편하게 처리할수 있기 떄문이다 
    // ForceMode2D.Impulse는 '충격량' 모드로, 질량의 영향을 받으며 순간적으로 힘을 팍! 주는 방식입니다.
    //AddForce: 물체를 미는 힘
    rigid.AddForce(Vector2.right * h , ForceMode2D.Impulse);

    // 오른쪽으로 너무 빠를 때
    // velocity = velocity: 물체의 현재 속도 자체, x축으로 얼마나 빠른지, y축으로 얼마나 빠른지를 동시에 가지고 있는 '화살표'라고 생각하면 된다 
    if (rigid.velocity.x > maxSpeed) //현재 이 물체가 오른쪽(x)으로 움직이는 속도가 내가 정한 한계치(maxSpeed)를 넘었는가?"를 확인하는 것
    {
        // y축을 0으로 잡아버리면 점프 했을때 공중에서 멈춰버린다 
        // x축 속도만 maxSpeed로 고정, y축은 현재 속도 유지
        rigid.velocity = new Vector2(maxSpeed, rigid.velocity.y); 
    }
    // 왼쪽으로 너무 빠를 때 (음수니까 더 작을 때를 체크)
    else if (rigid.velocity.x < -maxSpeed) 
    {
        // x축 속도만 -maxSpeed로 고정
        //-maxSpeed = 왼쪽 방향의 최대 속도값
        //rigid.velocity.y = 지금 현재의 수직 속도
        //X는 내가 정한 값으로 바꾸지만, Y(점프하거나 떨어지는 속도)는 현재 상태를 그대로 복사해서 써라는 뜻이다 
        //이렇게 vector를 만들어서 rigid.velocity에 넣어주면 원래가지고 있던 속도는 사라지고 현재만든 백터값이 들어간다 
        rigid.velocity = new Vector2(-maxSpeed, rigid.velocity.y);
    }

    if(rigid.velocity.y < 0) // 현재 수직속도가 0이면 동작 
    {
    //RayCast: 오브젝트 검색을 위해 Ray를 쏘는 방식
    //DrawRay: 에디터 상에서만 Ray를 그려주는 함수 
    //**Debug.DrawRay**는 게임 화면에는 보이지 않지만, **에디터(Scene 뷰)에서만 보이는 "개발자 전용 가이드 라인"**을 그리는 도구
    //캐릭터의 위치에서 발밑으로 초록색 선을 그어주는 아주 중요한 디버깅용 코드
    Debug.DrawRay(rigid.position, Vector3.down,new Color(0,1,0)); // 초록색 실선이 보인다
    //RayCastHit: Ray에 닿은 오브젝트
    //캐릭터가 발밑으로 **"보이지 않는 투명한 레이저"**를 쏴서, 그 레이저에 무언가 걸렸을 때 그 물체의 이름을 알려주는 코드
    //GetMask: 레이어 이름에 해당하는 정수값을 리턴하는 함수 
    // 1. 레이저를 쏴서 맞은 정보를 rayHit이라는 상자에 담습니다.
    RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, Vector3.down, 1,LayerMask.GetMask("Platform"));
    // 2. 만약 레이저가 무언가(collider)에 맞았다면?
    if (rayHit.collider != null)
    {
        //distance:ray에 닿았을 때의 거리 
        //이 코드는 캐릭터가 점프 후 내려올 때, 바닥과의 거리가 0.5m 이내로 가까워지면 '이제 착지했어!'라고 판단하고 점프 애니메이션을 꺼주는 역할을 합니다.
        if(rayHit.distance < 0.5f)
        // 맞은 물체의 이름을 콘솔창(Console)에 출력합니다.
        //  Debug.Log(rayHit.collider.name);
        anim.SetBool("isJumping",false); // 점프 모션을 끈다 

    }
    }
    
   
}
    
