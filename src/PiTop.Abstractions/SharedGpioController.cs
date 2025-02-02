﻿using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PiTop.Abstractions
{
    internal class SharedGpioController : IGpioController
    {
        private readonly List<int> _openPins = new List<int>();
        private readonly IGpioController _controller;

        public SharedGpioController(IGpioController controller)
        {
            _controller = controller;
        }

        public void Dispose()
        {
            foreach (var openPin in _openPins)
            {
                _controller.ClosePin(openPin);
            }
            _openPins.Clear();
        }

        public PinNumberingScheme NumberingScheme => _controller.NumberingScheme;

        public int PinCount => _controller.PinCount;

        public void OpenPin(int pinNumber)
        {
            try
            {
                _controller.OpenPin(pinNumber);
                _openPins.Add(pinNumber);
            }
            catch (IOException ex)
            {
                throw new IOException($"Error opening pin {pinNumber}: " + ex.Message, ex);
            }
        }

        public void OpenPin(int pinNumber, PinMode mode)
        {
            try
            {
                _controller.OpenPin(pinNumber, mode);
                _openPins.Add(pinNumber);
            }
            catch (IOException ex)
            {
                throw new IOException($"Error opening pin {pinNumber} as {mode}: " + ex.Message, ex);
            }
        }

        public void ClosePin(int pinNumber)
        {
            if (_openPins.Remove(pinNumber))
            {
                _controller.ClosePin(pinNumber);
            }
        }

        public void SetPinMode(int pinNumber, PinMode mode)
        {
            _controller.SetPinMode(pinNumber, mode);
        }

        public PinMode GetPinMode(int pinNumber)
        {
            return _controller.GetPinMode(pinNumber);
        }

        public bool IsPinOpen(int pinNumber)
        {
            return _openPins.Contains(pinNumber);
        }

        public bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            return _controller.IsPinModeSupported(pinNumber, mode);
        }

        public PinValue Read(int pinNumber)
        {
            return _controller.Read(pinNumber);
        }

        public void Write(int pinNumber, PinValue value)
        {
            _controller.Write(pinNumber, value);
        }

        public WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, TimeSpan timeout)
        {
            return _controller.WaitForEvent(pinNumber, eventTypes, timeout);
        }

        public WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            return _controller.WaitForEvent(pinNumber, eventTypes, cancellationToken);
        }

        public ValueTask<WaitForEventResult> WaitForEventAsync(int pinNumber, PinEventTypes eventTypes, TimeSpan timeout)
        {
            return _controller.WaitForEventAsync(pinNumber, eventTypes, timeout);
        }

        public ValueTask<WaitForEventResult> WaitForEventAsync(int pinNumber, PinEventTypes eventTypes, CancellationToken token)
        {
            return _controller.WaitForEventAsync(pinNumber, eventTypes, token);
        }

        public void RegisterCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            _controller.RegisterCallbackForPinValueChangedEvent(pinNumber, eventTypes, callback);
        }

        public void UnregisterCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            _controller.UnregisterCallbackForPinValueChangedEvent(pinNumber, callback);
        }

        public void Write(ReadOnlySpan<PinValuePair> pinValuePairs)
        {
            _controller.Write(pinValuePairs);
        }

        public void Read(Span<PinValuePair> pinValuePairs)
        {
            _controller.Read(pinValuePairs);
        }
    }
}