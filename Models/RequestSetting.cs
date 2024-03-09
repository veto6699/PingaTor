using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PingaTor.Models
{
    internal struct RequestSetting
    {
        /// <summary>
        /// Период запроса в миллисекундах
        /// </summary>
        public ulong Period { get; set; }
        /// <summary>
        /// Ожидаемый код ответа
        /// </summary>
        public ushort HTTPCode { get; set; }
        /// <summary>
        /// Timeout запроса в миллисекундах
        /// </summary>
        public ulong Timeout { get; set; }
        /// <summary>
        /// URL для запроса
        /// </summary>
        public string URL { get; set; }
        /// <summary>
        /// Метод отправки
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// Запрос в формате JSON
        /// </summary>
        public string Request { get; set; }
        /// <summary>
        /// Путь к файлу действий, используется если на запрос пришла ошибка
        /// </summary>
        public string FileAction { get; set; }
        /// <summary>
        /// Имя файла запроса
        /// </summary>
        public string NameRequest { get; set; }
        /// <summary>
        /// Эталон ответа
        /// </summary>
        [Obsolete("Теперь нужно использовать ResponseFragments")]
        public string? Reference
        {
            get
            {
                if (ResponseFragments is null)
                    return null;
                else
                    return ResponseFragments[0];
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (ResponseFragments is null)
                        ResponseFragments = new List<string>(1) { value };
                    else
                        ResponseFragments[0].Insert(0, value);
                }
            }
        }
        /// <summary>
        /// Набор фрагментов ответа которые должны быть в эталонном ответе
        /// </summary>
        public List<string> ResponseFragments { get; set; }
    }
}
