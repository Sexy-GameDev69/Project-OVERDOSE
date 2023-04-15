using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    
    public float rotationStep = 15f;

    public float zOffsetHeadArms = 250f;

    [Header("Important Stuff")]
    [Range(0, 1)]
    public float armsWeight = 1f;
    public float xOffsetLeftArm = -100f;
    public float xOffsetRightArm = 100f;
    public float timeForRaisingHands = 0.5f;
    public Transform rightWrist = null;
    public Transform leftWrist = null;

    [Header("Some more Important Stuff")]
    public float jumpForce = 100f;
    public GameObject mainPlayerCollider = null;
    public GameObject jumpingPlayerCollider = null;
    public LayerMask checkingLayers;
    public float checkDistance = 1f;
    public float delayBeforeCheck = 1f;

    [Header("Bullet Time Stuff")]
    [Range(0, 1)]
    public float slowedTime;
    public PostProcessVolume mainPPVolume = null;
    public PostProcessProfile mainPPProfile = null;
    public PostProcessProfile slowtimePPProfile = null;

    [Header("Some shooting stuff which is also important")]
    public Shooting shooting;

    private bool isRunning = false;
    private bool isAiming = false;
    private bool handsAreRaised = false;
    private bool isGrounded = true;
    private bool isStanding = true;
    private bool isTryingToStand = false;

    private float currentTimeForRisingHands = 0f;
    private float currentArmsWeight = 0f;
    private float neededTimeForGroundCheck = 0f;

    private Vector3 runDirection;
    private Vector3 crosshairPosition;
    private Vector3 crosshairWorldPositionWithOffsets;
    private Vector3 rightArmAimPosition;
    private Vector3 leftArmAimPosition;

    private Camera mainCamera = null;
    private Animator playerAnimator = null;
    private Transform playerTransform = null;
    private Rigidbody playerRigidbody = null;

    private void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        mainCamera = Camera.main;
        playerAnimator = GetComponent<Animator>();
        playerTransform = transform;
        playerRigidbody = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        runDirection.x = Input.GetAxis("Horizontal");
        runDirection.z = Input.GetAxis("Vertical");
        runDirection = runDirection.normalized;

        isRunning = runDirection.magnitude == 0 ? false : true;

        crosshairPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
        crosshairWorldPositionWithOffsets = mainCamera.ScreenToWorldPoint(new Vector3(crosshairPosition.x,
                                                                                      crosshairPosition.y,
                                                                                      zOffsetHeadArms));

        leftArmAimPosition = mainCamera.ScreenToWorldPoint(new Vector3(crosshairPosition.x + xOffsetLeftArm,
                                                                       crosshairPosition.y, zOffsetHeadArms));

        rightArmAimPosition = mainCamera.ScreenToWorldPoint(new Vector3(crosshairPosition.x + xOffsetRightArm,
                                                                        crosshairPosition.y, zOffsetHeadArms));


        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
        }

        if (Input.GetMouseButtonUp(1) && !Input.GetMouseButton(0))
        {
            isAiming = false;
        }

        if (Input.GetMouseButton(0))
        {
            isAiming = true;
            shooting.Shoot(crosshairWorldPositionWithOffsets);
        }

        if (Input.GetMouseButtonUp(0) && !Input.GetMouseButton(1))
        {
            isAiming = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded && isStanding)
            {
                Jump();
            }
            else if (isGrounded && !isStanding && !isTryingToStand)
            {
                isTryingToStand = true;
                GetUp();
            }
        }
    }

    private void FixedUpdate()
    {
       if (!isStanding && !isTryingToStand)
        {
            Vector3 aimDirection = mainCamera.transform.forward;
            aimDirection.y = 0f;
            aimDirection = aimDirection.normalized;
            aimDirection = playerTransform.InverseTransformDirection(aimDirection);

            playerAnimator.SetFloat("aimHorizontal", aimDirection.x);
            playerAnimator.SetFloat("aimVertical", aimDirection.z);

            CheckForGrounded();
        }
        else if (isGrounded && isStanding)
        {
            RotateToCrosshair();

            playerAnimator.SetFloat("moveHorizontal", runDirection.x);
            playerAnimator.SetFloat("moveVertical", runDirection.z);

            Vector3 runVector = playerTransform.TransformDirection(runDirection) * moveSpeed;
            runVector.y = playerRigidbody.velocity.y;

            playerRigidbody.velocity = runVector;
        }
    }

    private void LateUpdate()
    {
       if ((isAiming || !isStanding) && !isTryingToStand)
        {
            Vector3 rightDirection = crosshairWorldPositionWithOffsets - rightWrist.position;
            Vector3 leftDirection = crosshairWorldPositionWithOffsets - leftWrist.position;

            leftWrist.LookAt(leftDirection);
            rightWrist.LookAt(rightDirection);
        }
    }

   private void OnAnimatorIK()
    {
        playerAnimator.SetLookAtPosition(crosshairWorldPositionWithOffsets);
        playerAnimator.SetLookAtWeight(1.0f, 0.5f, 1.0f, 1.0f, 0.7f);


        if ((isAiming || !isStanding) && !isTryingToStand)
        {

            handsAreRaised = true;

            if (currentTimeForRisingHands < timeForRaisingHands)
            {
                currentTimeForRisingHands += Time.deltaTime;
                currentArmsWeight = Mathf.Lerp(0, armsWeight, currentTimeForRisingHands / timeForRaisingHands);
            }
            else
            {
                currentTimeForRisingHands = timeForRaisingHands;
                currentArmsWeight = armsWeight;
            }
        }
        else
        {
            if (!handsAreRaised)
            {
                return;
            }

            if (currentTimeForRisingHands > 0)
            {
                currentTimeForRisingHands -= Time.deltaTime;
                currentArmsWeight = Mathf.Lerp(0, armsWeight, currentTimeForRisingHands / timeForRaisingHands);
            }
            else
            {
                currentTimeForRisingHands = 0;
                currentArmsWeight = 0;
                handsAreRaised = false;
            }
        }

        playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentArmsWeight);
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentArmsWeight);

        playerAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightArmAimPosition);
        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftArmAimPosition);
    }

    private void RotateToCrosshair()
    {
        float cameraYRotation = mainCamera.transform.rotation.eulerAngles.y;

        playerTransform.rotation = Quaternion.Slerp(transform.rotation,
                                                    Quaternion.Euler(0f, cameraYRotation, 0f),
                                                    rotationStep * Time.fixedDeltaTime);
    }

   private void Jump()
    {
        playerRigidbody.velocity = Vector3.zero;

       isGrounded = false;
        isStanding = false;

       mainPlayerCollider.SetActive(false);
        jumpingPlayerCollider.SetActive(true);

        playerAnimator.SetTrigger("Jump");

        Vector3 jumpDirection;
        if (runDirection.magnitude == 0)
        {
            jumpDirection = playerTransform.forward;
        }
        else
        {
            jumpDirection = playerTransform.TransformDirection(runDirection);
        }
        jumpDirection = jumpDirection.normalized;

        playerRigidbody.AddForce((jumpDirection + Vector3.up) * playerRigidbody.mass * jumpForce);

        playerTransform.rotation = Quaternion.LookRotation(jumpDirection);

        SlowDownTime();
        neededTimeForGroundCheck = Time.time + delayBeforeCheck;
   }

    private void CheckForGrounded()
    {
        if (Time.time < neededTimeForGroundCheck) return;

        CapsuleCollider playerCapsCollider = jumpingPlayerCollider.GetComponent<CapsuleCollider>();
        Bounds playerCapsColliderBounds = playerCapsCollider.bounds;
        Vector3 offsetVector = new Vector3(0f, 0f, playerCapsCollider.height / 2);

        Ray[] checkingRays = {
                                new Ray(playerCapsColliderBounds.center, Vector3.down),
                                new Ray(playerCapsColliderBounds.center + offsetVector, Vector3.down),
                                new Ray(playerCapsColliderBounds.center - offsetVector, Vector3.down),
                             };

        foreach (Ray currentRay in checkingRays)
        {
            isGrounded |= Physics.Raycast(currentRay, checkDistance, checkingLayers);
        }

        if (isGrounded)
        {
            NormalizeTime();
        }
    }

    private void SlowDownTime()
    {
        mainPPVolume.profile = slowtimePPProfile;
        Time.timeScale = slowedTime;
    }

    private void NormalizeTime()
    {
        mainPPVolume.profile = mainPPProfile;
        Time.timeScale = 1f;
    }

    private void GetUp()
    {
        shooting.canShoot = false;
        isAiming = false;

        playerAnimator.SetTrigger("GetUp");

        mainPlayerCollider.SetActive(true);
        jumpingPlayerCollider.SetActive(false);
    }

    public void ChangeIsStandingFromAnimator()
    {
        isTryingToStand = false;
        isStanding = true;
        shooting.canShoot = true;
    }
}
