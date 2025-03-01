using System;

/// <summary>
/// Represents a simplified GPT response. Modify according to the actual API response.
/// </summary>
/// 
namespace Modules.GPTService.Internal
{
    using System;

    [Serializable]
    public class GPTMessage
    {
        public string role;
        public string content;
    }



    [Serializable]
    public class GPTChoice
    {
        public GPTMessage message;
    }

    [Serializable]
    public class GPTResponse
    {
        public GPTChoice[] choices;
    }

}