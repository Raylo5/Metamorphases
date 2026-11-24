// Punto único de acceso a los controles (InputSystem_Actions, generado por Unity
// a partir de Assets/InputSystem_Actions.inputactions).
// Cualquier script puede leer: GameInput.Actions.Player.Jump.WasPressedThisFrame()
public static class GameInput
{
    static InputSystem_Actions actions;

    public static InputSystem_Actions Actions
    {
        get
        {
            if (actions == null)
            {
                actions = new InputSystem_Actions();
                actions.Enable();
            }
            return actions;
        }
    }

    // Para la pausa (T020): apaga/enciende los controles del jugador,
    // excepto "Pause", que tiene que seguir funcionando para poder salir de la pausa.
    public static void SetPlayerControls(bool enabled)
    {
        foreach (var action in Actions.Player.Get().actions)
        {
            if (action == Actions.Player.Pause) continue;
            if (enabled) action.Enable();
            else action.Disable();
        }
    }
}
