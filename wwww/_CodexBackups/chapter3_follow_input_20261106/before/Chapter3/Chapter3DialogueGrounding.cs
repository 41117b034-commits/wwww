// Only facts of this scene are available when the configured service is offline.
public static class Chapter3DialogueGrounding
{
    public static string SceneAnswer(string text)
    {
        if(text.Contains("我叫")||text.Contains("我的名字"))return "你還沒告訴過我你的名字。你希望我怎麼叫你？";
        if(text.Contains("你叫")||text.Contains("你的名字")||text.Contains("你是誰"))return "我是巴萬·拿威，和你一起行動的同伴。";
        if(text.Contains("哪")||text.Contains("那裡")||text.Contains("目的")||text.Contains("接下來")||text.Contains("駐在所"))
            return "這裡是馬赫坡駐在所。我們先聽莫那的安排，再沿山路去霧社學校外和達多會合。";
        if(text.Contains("比胡"))return "比胡·帖木是我們的同伴，他會和我們一起走。";
        if(text.Contains("莫那"))return "莫那·魯道在前面建築旁，等一下我們一起去聽他的安排。";
        if(text.Contains("武器")||text.Contains("刀")||text.Contains("槍"))return "莫那會讓你選獵刀或村田十八年式步槍，只能帶一種。";
        if(text.Contains("為什麼")||text.Contains("為何"))return "族人長期受勞役與欺壓，才決定起事。眼前先聽莫那安排，大家彼此照應。";
        if(text.Contains("怕")||text.Contains("緊張")||text.Contains("擔心"))return "我也會緊張。跟緊我們，有事就叫我。";
        return null;
    }
    public static string OpeningReply(string text)
    {
        if(!text.Contains("?")&&!text.Contains("？")&&(text.Contains("我會跟")||text.Contains("我会跟")||text.Contains("照應")||text=="好"||text=="好的"))return "好，保持間距，別烙單了!";
        string answer=SceneAnswer(text);if(answer!=null)return answer;
        return "這個問題我現在答不上來。先去見莫那，聽聽他的安排。";
    }
}
