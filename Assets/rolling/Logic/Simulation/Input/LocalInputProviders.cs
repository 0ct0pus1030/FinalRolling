using FGLogic.Core;
using FGLogic.Input;
using UnityEngine;

public static class LocalInputProvider
{
    static bool TEST_MODE = false;

    
    static (int frame, float h, float v, int buttons)[] testScript = new[] {
        (0, 1f, 0f, 0),                    // 0-29
        (30, 0f, 1f, 0),                   // 30-59
        (60, 0f, 0f, ButtonFlags.Attack),  // 60
        (61, -1f, 0f, 0),                  // 61-89
        (90, 0f, 0f, ButtonFlags.Attack),  // 90:
        (91, 0f, 0f, 0),                   // 91-100: ֹͣ
    };

    static int testIndex = 0;
    static float currentH = 0f;
    static float currentV = 0f;
    static int currentButtons = 0;

    public static FrameInput GetLocalInput(int frameId, int myPlayerId)
    {
        
        if (TEST_MODE && myPlayerId == 0)
        {
            if (testIndex < testScript.Length && testScript[testIndex].frame == frameId)
            {
                var cmd = testScript[testIndex];
                currentH = cmd.h;
                currentV = cmd.v;
                currentButtons = cmd.buttons;
                testIndex++;

                Debug.Log($"[AutoTest] Frame {frameId}: Move=({currentH},{currentV}) Buttons={currentButtons}");
            }

            if (currentButtons != 0)
            {
                int btn = currentButtons;
                currentButtons = 0;
                return CreateInput(frameId, myPlayerId, currentH, currentV, btn);
            }

            return CreateInput(frameId, myPlayerId, currentH, currentV, 0);
        }

       
        var input = new FrameInput
        {
            FrameId = frameId,
            PlayerId = myPlayerId
        };

        float h = 0f, v = 0f;

       
        if (Input.GetJoystickNames().Length > 0)
        {
            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");
        }

        
        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h = 1f;
        if (Input.GetKey(KeyCode.W)) v = 1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;

       
        if (Mathf.Abs(h) < 0.3f) h = 0f;
        if (Mathf.Abs(v) < 0.3f) v = 0f;

        var stickTemp = new FixedVector2(Fixed.FromFloat(h), Fixed.FromFloat(v));

        
        int dir = Direction.FromStick(stickTemp);

        
        input.Stick = Direction.ToVector(dir);

        
        if (Input.GetKey(KeyCode.J)) input.Buttons |= ButtonFlags.Attack;
        if (Input.GetKey(KeyCode.K)) input.Buttons |= ButtonFlags.Roll;
        if (Input.GetKey(KeyCode.L)) input.Buttons |= ButtonFlags.Block;

        return input;
    }

    static FrameInput CreateInput(int frameId, int playerId, float h, float v, int buttons)
    {
        
        if (Mathf.Abs(h) < 0.3f) h = 0f;
        if (Mathf.Abs(v) < 0.3f) v = 0f;

        var stickTemp = new FixedVector2(Fixed.FromFloat(h), Fixed.FromFloat(v));
        int dir = Direction.FromStick(stickTemp);

        return new FrameInput
        {
            PlayerId = playerId,
            FrameId = frameId,
            Stick = Direction.ToVector(dir),  
            Buttons = buttons
        };
    }
}