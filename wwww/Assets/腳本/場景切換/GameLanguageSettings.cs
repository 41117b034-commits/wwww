using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameSubtitleLanguage
{
    Chinese = 0,
    Japanese = 1,
    Seediq = 2,
    None = 3
}

public static class GameLanguageSettings
{
    private const string LanguagePreferenceKey = "WusheEvent.SubtitleLanguage";

    private static readonly Dictionary<string, string> JapaneseText =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "字幕", "字幕" },
            { "旁白", "ナレーション" },
            { "任務", "任務" },
            { "系統", "システム" },
            { "動作", "動作" },
            { "新郎", "花婿" },
            { "新娘", "花嫁" },
            { "賓客", "客" },
            { "族人", "部族の人" },
            { "族人長者", "部族の長老" },
            { "女性族人", "部族の女性" },
            { "日警", "日本人警察官" },
            { "你", "あなた" },

            { "1930.10.7，霧社。火光照亮婚禮，鼓聲和歌聲在山間回盪。", "1930年10月7日、霧社。婚礼の火が夜を照らし、太鼓と歌声が山あいに響いていた。" },
            { "你睜開眼，看見族人圍著火堆歌舞。今晚本該只是祝福新人的夜晚。", "目を開けると、部族の人々がたき火を囲んで歌い踊っている。今夜は本来、新郎新婦を祝うだけの夜のはずだった。" },
            { "朋友，今晚人多。跟著橘紅色標記拿酒送給賓客，再跟著黃綠色標記把食物分給族人；忙完再到舞圈一起跳吧。", "友よ、今夜は客が多い。橙色の印を追って酒を客へ届け、黄緑色の印を追って食べ物を皆に配ってくれ。終わったら踊りの輪に入ろう。" },
            { "你拿起一瓶酒。現在自己走到尚未收到酒的賓客旁邊。", "酒を一本手に取った。まだ酒を受け取っていない客のところへ行こう。" },
            { "你拿起食物。現在自己走到尚未收到食物的族人旁邊。", "食べ物を手に取った。まだ受け取っていない部族の人のところへ行こう。" },
            { "謝謝你，這杯我收下了。願祖靈庇佑新人。", "ありがとう。この一杯、ありがたくいただく。祖霊が新郎新婦を守ってくださいますように。" },
            { "謝謝你。今晚能這樣相聚，已經很難得。", "ありがとう。今夜こうして皆で集まれたことが、何よりうれしい。" },
            { "今天是新人的日子，先把煩惱放在山路外吧。", "今日は新郎新婦の日だ。悩み事は山道の向こうへ置いてこよう。" },
            { "火堆亮起來，祖靈會看見我們的歌聲。", "火が明るく燃えれば、祖霊にも私たちの歌が届くだろう。" },
            { "今晚一定要喝醉！", "今夜は思いきり飲もう！" },
            { "願祖靈庇佑新人。", "祖霊が新郎新婦を守ってくださいますように。" },
            { "辛苦了。事情都忙得差不多了，來舞圈一起跳吧。", "ご苦労だった。用事もほとんど済んだ。踊りの輪に入って一緒に踊ろう。" },
            { "來，跟著鼓聲一起踏步。今晚讓祖靈聽見我們的歌。", "さあ、太鼓に合わせて足を踏み鳴らそう。今夜は祖霊に私たちの歌を届けるんだ。" },
            { "朋友，先幫我把酒送給賓客，也把食物分給族人，再一起來跳舞。", "友よ、先に客へ酒を届け、皆に食べ物を配ってくれ。それから一緒に踊ろう。" },
            { "送酒與分享食物都完成了，去舞圈加入舞蹈吧。", "酒と食べ物を配り終えた。踊りの輪に加わろう。" },
            { "你先把手上的物品放回去了。", "手に持っていた物を元の場所へ戻した。" },
            { "你把酒遞給眼前的賓客。", "目の前の客に酒を手渡した。" },
            { "你把食物遞給眼前的族人。", "目の前の部族の人に食べ物を手渡した。" },
            { "我已經拿到酒了，先送給其他人吧。", "私はもう酒を受け取った。他の人に届けてあげてくれ。" },
            { "我已經拿到食物了，分給其他人吧。", "私はもう食べ物を受け取った。他の人に分けてあげてくれ。" },
            { "食物已經分得差不多了，謝謝你。", "食べ物はもう十分に行き渡った。ありがとう。" },
            { "酒已經送得差不多了，去看看還有沒有族人需要食物。", "酒はもう十分に行き渡った。食べ物を必要としている人がいないか見てきてくれ。" },
            { "還有幾位賓客沒有酒，送完再一起跳舞。", "まだ酒を受け取っていない客がいる。届け終えたら一緒に踊ろう。" },
            { "先把食物分給族人，忙完再一起跳舞。", "まず皆に食べ物を配ってくれ。終わったら一緒に踊ろう。" },
            { "先等目前的互動動作結束。", "今の動作が終わるまで待ってください。" },
            { "先完成送酒、分享食物與舞蹈，導火線事件才會發生。", "酒配り、食べ物配り、踊りをすべて終えると、次の事件へ進みます。" },

            { "歡笑聲戛然而止。遠處，只剩皮靴踩過山路的聲音。", "笑い声が突然途絶えた。遠くから、革靴が山道を踏む音だけが近づいてくる。" },
            { "遠處的山路上，兩名日警正朝著婚禮會場緩步逼近。", "遠くの山道から、二人の日本人警察官が婚礼会場へゆっくり近づいてくる。" },
            { "鼓聲突然慢了下來。山路傳來急促的皮靴聲，兩名日本警察闖進婚禮會場。", "太鼓の音が突然遅くなった。山道に慌ただしい革靴の音が響き、二人の日本人警察官が婚礼会場へ踏み込んできた。" },
            { "這種野蠻婚禮，竟然還敢辦得這麼熱鬧？", "こんな野蛮な婚礼を、よくもこれほど騒がしく開けたものだな。" },
            { "都給我安靜。你們最好記住自己的身分。", "全員静かにしろ。自分たちの身分を忘れるな。" },
            { "我們只是辦婚禮，沒有冒犯。", "私たちは婚礼をしているだけです。誰にも迷惑はかけていません。" },
            { "警察推倒酒杯，又粗暴地推開靠近的族人。", "警察官は杯をなぎ倒し、近づいた部族の人を乱暴に突き飛ばした。" },
            { "另一名警察把目光轉向一名女性族人，伸手逼近她。周圍的族人立刻騷動起來。", "もう一人の警察官は部族の女性に目を向け、手を伸ばして迫った。周囲はたちまち騒然となった。" },
            { "一名警察試圖騷擾女性族人，四周的怒氣瞬間升高。", "警察官が部族の女性に乱暴しようとし、周囲の怒りが一気に高まった。" },
            { "放開我！", "放して！" },
            { "你要怎麼做？", "どうする？" },
            { "上前阻止", "前に出て止める" },
            { "沉默觀望", "黙って見守る" },
            { "你要怎麼做？按 1 上前阻止，按 2 沉默觀望。", "どうする？ 1で前に出て止める、2で黙って見守る。" },
            { "夠了！不要再羞辱我們！", "もう十分だ！ これ以上、私たちを侮辱するな！" },
            { "警察冷笑著抬手，把桌邊的酒杯掃倒。", "警察官は薄笑いを浮かべて手を上げ、卓上の杯を払い落とした。" },
            { "日警的手掃向桌邊。", "日本人警察官の手が卓上へ伸びた。" },
            { "一名族人上前質問，立刻被粗暴地推開。", "一人の部族民が問いただそうと前へ出たが、すぐに乱暴に突き飛ばされた。" },
            { "你沒有上前。警察強行拉著女性族人往小木屋走去。", "あなたは前に出なかった。警察官は部族の女性を無理やり小屋へ連れていった。" },
            { "木屋門關上後，裡面傳出痛苦的叫喊聲。屋外的人全都僵在原地。", "小屋の扉が閉まると、中から苦痛に満ちた叫び声が聞こえた。外の人々はその場で凍りついた。" },
            { "你沉默地站在原地。警察把女性族人帶向木屋，屋內隨後傳出痛苦的叫喊聲。", "あなたは黙って立ち尽くした。警察官が部族の女性を小屋へ連れていき、やがて中から苦痛の叫び声が聞こえた。" },
            { "幾名族人憤而衝上前，聯手把警察推開，混亂中拳腳相向。", "怒った数人の部族民が駆け寄り、力を合わせて警察官を押し返した。混乱の中で殴り合いが始まった。" },
            { "砰——槍聲突然響起。幾名族人在混亂中倒下，所有人瞬間停住。", "銃声が突然響いた。混乱の中で数人の部族民が倒れ、全員の動きが一瞬で止まった。" },
            { "兩名警察整理衣服，轉身沿著山路離開。婚禮現場只剩火堆與沉默。", "二人の警察官は服を整え、山道を引き返した。婚礼会場には、たき火と沈黙だけが残った。" },
            { "今天的事，族人不會忘記。", "今日のことを、我々は決して忘れない。" },
            { "族人望著日警下山的背影。憤怒留在每個人的眼神裡，卻沒有人知道下一步該怎麼辦。", "人々は山を下る警察官の背中を見つめていた。全員の目に怒りが残っていたが、次に何をすべきかは誰にも分からなかった。" }
        };

    public static GameSubtitleLanguage CurrentLanguage
    {
        get
        {
            if (!PlayerPrefs.HasKey(LanguagePreferenceKey))
            {
                PlayerPrefs.SetInt(LanguagePreferenceKey, (int)GameSubtitleLanguage.Chinese);
                PlayerPrefs.Save();
            }

            int storedValue = PlayerPrefs.GetInt(
                LanguagePreferenceKey,
                (int)GameSubtitleLanguage.Chinese);

            if (!Enum.IsDefined(typeof(GameSubtitleLanguage), storedValue))
            {
                storedValue = (int)GameSubtitleLanguage.Chinese;
            }

            return (GameSubtitleLanguage)storedValue;
        }
    }

    public static bool SubtitlesEnabled
    {
        get { return CurrentLanguage != GameSubtitleLanguage.None; }
    }

    public static void SetLanguage(GameSubtitleLanguage language)
    {
        PlayerPrefs.SetInt(LanguagePreferenceKey, (int)language);
        PlayerPrefs.Save();
    }

    public static string LocalizeSpeaker(string chineseText)
    {
        if (CurrentLanguage == GameSubtitleLanguage.Japanese)
        {
            return Lookup(JapaneseText, chineseText);
        }

        return chineseText;
    }

    public static string LocalizeSubtitle(string chineseText)
    {
        switch (CurrentLanguage)
        {
            case GameSubtitleLanguage.Japanese:
                return Lookup(JapaneseText, chineseText);
            case GameSubtitleLanguage.Seediq:
                // The Wushe area uses Tgdaya Seediq. Keep the source visible until
                // the project's language adviser supplies an approved translation.
                return "<color=#E6B85C>【Kari Seediq 翻譯待校訂】</color>\n" + chineseText;
            default:
                return chineseText;
        }
    }

    public static string LocalizeInterfaceText(string chineseText)
    {
        return CurrentLanguage == GameSubtitleLanguage.Japanese
            ? Lookup(JapaneseText, chineseText)
            : chineseText;
    }

    private static string Lookup(Dictionary<string, string> table, string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        string translated;
        return table.TryGetValue(source, out translated) ? translated : source;
    }
}
