using UnityEngine;

// Supporting names are fictional, composed from the CIP Seediq name register.
// Keep the four user-authored council identities and their subtitle role labels.
public static class Chapter2CastNames
{
    static void Set(Chapter2Actor actor,string name,string roman,string role,bool child=false)
    {
        if(!actor)return;
        actor.characterName=actor.name=name;actor.romanizedName=roman;
        actor.dialogueRole=role;actor.isChild=child;
    }
    public static void Apply(Chapter2Controller c)
    {
        Set(c.workers[0],"阿威·比胡","Awi Pihu","你是熟悉林道的賽德克青年，負責帶玩家前往巨木。珍惜森林，說話沉穩，願意介紹眼前環境。");
        Set(c.workers[1],"達奇斯·諾幹","Dakis Nokan","你是在巨木附近勞作的賽德克青年。關心同伴與家人，珍惜森林。");
        Set(c.workers[2],"拉娃·塔道","Lawa Tado","你是在林間活動的賽德克女子。關心同伴、家人與糧食，說話溫和。");
        Set(c.officer,"中村正雄","Nakamura Masao","你是現場督工的日本警察。語氣簡短嚴肅，要求族人服從伐木命令，但不透露未發生的劇情。");
        var group=c.dayGroup.transform.Find("Background people · local patrols");
        if(group)foreach(var actor in group.GetComponentsInChildren<Chapter2Actor>(true))
        {
            switch(actor.name)
            {
                case "林間族人 2": case "帖木·阿班":
                    Set(actor,"帖木·阿班","Temu Aban","你是林邊休息的賽德克男子。勞作後疲憊，說話溫和，關心糧食、工資與家人。");break;
                case "林間族人 3": case "伊婉·諾幹":
                    Set(actor,"伊婉·諾幹","Iwan Nokan","你是賽德克女子，熟悉林間植物與生活，關心家人，願意和部落青年交談。");break;
                case "樹旁巡察警察 1": case "佐藤正一":
                    Set(actor,"佐藤正一","Sato Shoichi","你是巡查林地的日本警察。嚴肅寡言，關心秩序，只談自己知道的事情。");break;
                case "樹旁巡察警察 2": case "田中武雄":
                    Set(actor,"田中武雄","Tanaka Takeo","你是林道執勤的日本警察。說話簡短，留意通行與搬運，不知道上級未告知的事情。");break;
                case "林間原住民小孩": case "都比·阿威":
                    SetChild(actor);break;
            }
        }
        Set(c.mona,"莫那·魯道","Mona Rudo","賽德克社頭目");
        Set(c.tado,"達多·莫那","Tado Mona","莫那·魯道之子");
        Set(c.bawan,"巴萬·拿威","Bawan Nawi","年輕戰士");
        Set(c.watan,"瓦旦","Watan","長老代表");
        Set(c.leaders[2],"古慕·阿基","Kumu Aki","出席密議的賽德克女子");
        Set(c.leaders[4],"伊婉·阿比斯","Iwan Abis","出席密議的賽德克女長輩");
        Set(c.leaders[5],"露比·巴干","Lubi Bakan","出席密議的賽德克女子");
        Set(c.conservatives[0],"比胡·帖木","Pihu Teymu","出席密議的賽德克青年");
        Set(c.conservatives[1],"歐冰·拿威","Obing Nawi","出席密議的賽德克女子");
        foreach(var actor in c.nightGroup.GetComponentsInChildren<Chapter2Actor>(true))
            if(actor.name=="會議原住民小孩"||actor.name=="都比·阿威")SetChild(actor);
    }
    static void SetChild(Chapter2Actor actor)=>Set(actor,"都比·阿威","Dupi Awi","你是跟著家人在林邊活動的賽德克小孩。好奇、說話簡單，關心家人和動植物，不懂軍事政治。",true);
}
