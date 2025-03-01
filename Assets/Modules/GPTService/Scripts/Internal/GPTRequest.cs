using System;

/// <summary>
/// Represents the GPT request payload. Adjust fields as needed.
/// </summary>
/// 
namespace Modules.GPTService.Internal
{
    [Serializable]
    public class GPTRequest
    {
        public string model;
        public GPTMessage[] messages;
    }
}