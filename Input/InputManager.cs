﻿using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BrewLib.Input
{
    public class InputManager : IDisposable
    {
        private InputHandler handler;
        private GameWindow window;

        private bool hadMouseFocus;
        private bool hasMouseHover;
        public bool HasMouseFocus => window.Focused && hasMouseHover;
        public bool HasWindowFocus => window.Focused;

        public MouseState Mouse => OpenTK.Input.Mouse.GetCursorState();
        public KeyboardState Keyboard => OpenTK.Input.Keyboard.GetState();

        private Dictionary<int, GamepadManager> gamepadManagers = new Dictionary<int, GamepadManager>();
        public IEnumerable<GamepadManager> GamepadManagers => gamepadManagers.Values;
        public GamepadManager GetGamepadManager(int gamepadIndex = 0) => gamepadManagers[gamepadIndex];

        // Helpers
        public Vector2 MousePosition
        {
            get
            {
                var clientCoords = window.PointToClient(new Point(Mouse.X, Mouse.Y));
                return new Vector2(clientCoords.X, clientCoords.Y);
            }
        }

        public bool Control { get; private set; }
        public bool Shift { get; private set; }
        public bool Alt { get; private set; }

        public bool ControlOnly => Control && !Shift && !Alt;
        public bool ShiftOnly => !Control && Shift && !Alt;
        public bool AltOnly => !Control && !Shift && Alt;

        public bool ControlShiftOnly => Control && Shift && !Alt;
        public bool ControlAltOnly => Control && !Shift && Alt;
        public bool ShiftAltOnly => !Control && Shift && Alt;

        public InputManager(GameWindow window, InputHandler handler)
        {
            this.window = window;
            this.handler = handler;

            window.FocusedChanged += window_FocusedChanged;
            window.MouseEnter += window_MouseEnter;
            window.MouseLeave += window_MouseLeave;

            window.MouseUp += window_MouseUp;
            window.MouseDown += window_MouseDown;
            window.MouseWheel += window_MouseWheel;
            window.MouseMove += window_MouseMove;
            window.KeyDown += window_KeyDown;
            window.KeyUp += window_KeyUp;
            window.KeyPress += window_KeyPress;
        }

        public void Dispose()
        {
            foreach (var gamepadIndex in new List<int>(gamepadManagers.Keys))
                DisableGamepadEvents(gamepadIndex);
            gamepadManagers.Clear();

            window.FocusedChanged -= window_FocusedChanged;
            window.MouseEnter -= window_MouseEnter;
            window.MouseLeave -= window_MouseLeave;

            window.MouseUp -= window_MouseUp;
            window.MouseDown -= window_MouseDown;
            window.MouseWheel -= window_MouseWheel;
            window.MouseMove += window_MouseMove;
            window.KeyDown -= window_KeyDown;
            window.KeyUp -= window_KeyUp;
            window.KeyPress -= window_KeyPress;
        }

        public void EnableGamepadEvents(int gamepadIndex = 0)
        {
            var manager = new GamepadManager(gamepadIndex);
            manager.OnConnected += gamepadManager_OnConnected;
            manager.OnButtonDown += gamepadManager_OnButtonDown;
            manager.OnButtonUp += gamepadManager_OnButtonUp;
            gamepadManagers.Add(gamepadIndex, manager);
        }

        public void DisableGamepadEvents(int gamepadIndex = 0)
        {
            var manager = gamepadManagers[gamepadIndex];
            manager.OnConnected -= gamepadManager_OnConnected;
            manager.OnButtonDown -= gamepadManager_OnButtonDown;
            manager.OnButtonUp -= gamepadManager_OnButtonUp;
            gamepadManagers.Remove(gamepadIndex);
        }

        public void Update()
        {
            foreach (var gamepadManager in gamepadManagers.Values)
                gamepadManager.Update();
        }

        private void updateMouseFocus()
        {
            if (hadMouseFocus != HasMouseFocus)
                hadMouseFocus = HasMouseFocus;

            handler.OnFocusChanged(new FocusChangedEventArgs(HasMouseFocus));
        }
        private void window_MouseEnter(object sender, EventArgs e)
        {
            hasMouseHover = true;
            updateMouseFocus();
        }
        private void window_MouseLeave(object sender, EventArgs e)
        {
            // https://github.com/OpenTK/OpenTK/issues/301
            return;

            hasMouseHover = false;
            updateMouseFocus();
        }
        private void window_FocusedChanged(object sender, EventArgs e) => updateMouseFocus();

        private void window_MouseDown(object sender, MouseButtonEventArgs e) => handler.OnClickDown(e);
        private void window_MouseUp(object sender, MouseButtonEventArgs e) => handler.OnClickUp(e);
        private void window_MouseMove(object sender, MouseMoveEventArgs e) => handler.OnMouseMove(e);

        private void updateModifierState(KeyboardKeyEventArgs e)
        {
            Control = e.Modifiers.HasFlag(KeyModifiers.Control);
            Shift = e.Modifiers.HasFlag(KeyModifiers.Shift);
            Alt = e.Modifiers.HasFlag(KeyModifiers.Alt);
        }
        private void window_KeyDown(object sender, KeyboardKeyEventArgs e) { updateModifierState(e); handler.OnKeyDown(e); }
        private void window_KeyUp(object sender, KeyboardKeyEventArgs e) { updateModifierState(e); handler.OnKeyUp(e); }
        private void window_KeyPress(object sender, KeyPressEventArgs e) => handler.OnKeyPress(e);

        private void window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            handler.OnMouseWheel(e);
        }

        private void gamepadManager_OnConnected(object sender, GamepadEventArgs e) => handler.OnGamepadConnected(e);
        private void gamepadManager_OnButtonDown(object sender, GamepadButtonEventArgs e) => handler.OnGamepadButtonDown(e);
        private void gamepadManager_OnButtonUp(object sender, GamepadButtonEventArgs e) => handler.OnGamepadButtonUp(e);
    }

    public class FocusChangedEventArgs : EventArgs
    {
        private bool hasFocus;
        public bool HasFocus => hasFocus;

        public FocusChangedEventArgs(bool hasFocus)
        {
            this.hasFocus = hasFocus;
        }
    }
}
