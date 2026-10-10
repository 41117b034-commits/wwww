using System;
// Only the unchanged dialogue-message DTO is needed for isolated adapter checks.
public class Chapter2LocalDialogue
{
    [Serializable] public class Message
    {
        public string role, content;
        public Message(string r, string c) { role = r; content = c; }
    }
}
