

using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;

// Rigidbody2D 컴포넌트를 담을 변수를 선언
RigidBody2D rigid;
public float maxSpeed;
SpriteRenderer spriteRenderer;

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

}


void Update()
{
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
    spriteRenderer.flipX = 
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
}
    
