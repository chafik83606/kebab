using UnityEngine;

/// <summary>
/// Relie joysticks tactiles au PlayerController.
/// </summary>
public class MobileInputBridge : MonoBehaviour
{
    public MobileJoystick moveJoystick;
    public MobileLookPad lookPad;
    public PlayerController player;

    private void Update()
    {
        if (player == null)
            player = PlayerController.Instance;
        if (player == null) return;

        if (moveJoystick != null)
            player.mobileMoveInput = new Vector2(moveJoystick.Value.x, moveJoystick.Value.y);
        else
            player.mobileMoveInput = Vector2.zero;

        if (lookPad != null)
            player.mobileLookInput = lookPad.LookDelta;
        else
            player.mobileLookInput = Vector2.zero;
    }
}
