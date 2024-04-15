﻿using System.Data;
using System.Runtime.InteropServices;

namespace TusurUI.Source
{
    public class PowerSupply
    {
        [DllImport("Libs/PowerSupply.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int PowerSupply_Connect(string port);

        [DllImport("Libs/PowerSupply.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PowerSupply_SetCurrentVoltage(ushort current, ushort voltage);

        [DllImport("Libs/PowerSupply.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PowerSupply_ReadCurrent();

        [DllImport("Libs/PowerSupply.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PowerSupply_TurnOn();

        [DllImport("Libs/PowerSupply.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PowerSupply_TurnOff();

        [DllImport("Libs/PowerSupply.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PowerSupply_ResetZP();

        PowerSupply() { }

        public static int Connect(string port) { return PowerSupply_Connect(port); }
        public static int TurnOn() { return PowerSupply_TurnOn(); }
        public static int TurnOff() { return PowerSupply_TurnOff(); }
        public static int SetCurrentVoltage(ushort current, ushort voltage) { return PowerSupply_SetCurrentVoltage(current, voltage); }
        public static int ReadCurrent() { return PowerSupply_ReadCurrent(); }
        public static int ResetZP() { return PowerSupply_ResetZP(); }

        private static string GetErrorMessageEN(int errorCode)
        {
            return errorCode switch
            {
                0 => "Operation successful.",
                1 => "Failed to initialize connection.",
                2 => "Failed to set device as slave.",
                3 => "Failed to connect to the device.",
                4 => "Failed to set current setpoint.",
                5 => "Failed to set voltage setpoint.",
                6 => "Failed to turn on the power supply.",
                7 => "Failed to activate work mode.",
                8 => "Failed to reset current setpoint.",
                9 => "Failed to reset voltage setpoint.",
                10 => "Failed to reset work mode.",
                11 => "Failed to turn off the power supply.",
                12 => "Failed to reset ZP register (36).",
                _ => "Unknown error."
            };
        }
        private static string GetErrorMessageRU(int errorCode)
        {
            return errorCode switch
            {
                0 => "Операция прошла успешно.",
                1 => "Не удалось инициализировать соединение.",
                2 => "Не удалось установить устройство как slave.",
                3 => "Не удалось подключиться к устройству.",
                4 => "Не удалось установить уставку тока.",
                5 => "Не удалось установить уставку напряжения.",
                6 => "Не удалось включить блок питания.",
                7 => "Не удалось активировать рабочий режим.",
                8 => "Не удалось сбросить уставку тока.",
                9 => "Не удалось сбросить уставку напряжения.",
                10 => "Не удалось сбросить рабочий режим.",
                11 => "Не удалось выключить блок питания.",
                12 => "Не удалось сбросить регистр ЗП(36).",
                _ => "Неизвестная ошибка."
            };
        }
        public static string GetErrorMessage(int errorCode, string language = "RU")
        {
            string message = language switch
            {
                "RU" => GetErrorMessageRU(errorCode),
                "EN" => GetErrorMessageEN(errorCode),
                _ => "Language not supported."
            };

            return language == "RU" ? $"PowerSupply.dll: Код ошибки: {errorCode}. Сообщение: {message}"
                : $"PowerSupply.dll:Error code: {errorCode}. Message: {message}";
        }
    }
}
