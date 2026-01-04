using EggLink.DanhengServer.Internationalization;
using EggLink.DanhengServer.Database.Inventory; // 处理 ItemData 依赖

namespace EggLink.DanhengServer.Command.Command.Cmd;

[CommandInfo("mail", "Game.Command.Mail.Desc", "Game.Command.Mail.Usage", permission: "egglink.manage")]
public class CommandMail : ICommand
{
    [CommandDefault]
    public async ValueTask Mail(CommandArg arg)
    {
        // 1. 检查目标玩家是否存在
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        // 2. 检查基础参数数量是否足够
        if (arg.Args.Count < 7)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        // 3. 必须包含标题和内容标识符
        if (!(arg.Args.Contains("_TITLE") && arg.Args.Contains("_CONTENT")))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        // 4. 解析基础变量
        var sender = arg.Args[0];
        if (!int.TryParse(arg.Args[1], out var templateId)) templateId = 0;
        if (!int.TryParse(arg.Args[2], out var expiredDay)) expiredDay = 30;

        var title = "";
        var content = "";
        var attachments = new List<ItemData>();

        var flagTitle = false;
        var flagContent = false;
        var flagAttach = false;

        // 5. 循环解析所有参数（支持空格拼接标题和内容，支持附件解析）
        foreach (var text in arg.Args)
        {
            switch (text)
            {
                case "_TITLE":
                    flagTitle = true; flagContent = false; flagAttach = false;
                    continue;
                case "_CONTENT":
                    flagTitle = false; flagContent = true; flagAttach = false;
                    continue;
                case "_ATTACH":
                    flagTitle = false; flagContent = false; flagAttach = true;
                    continue;
            }

            if (flagTitle) title += text + " ";
            if (flagContent) content += text + " ";

            // 6. 附件解析逻辑：ID:数量
            if (flagAttach)
            {
                var parts = text.Split(':');
                if (parts.Length == 2 && uint.TryParse(parts[0], out var id) && uint.TryParse(parts[1], out var count))
                {
                    attachments.Add(new ItemData 
                    { 
                        // 显式强转解决 CS0266 错误
                        ItemId = (int)id, 
                        // 在业务层包装类中通常为 Count
                        Count = (int)count 
                    });
                }
            }
        }

        // 去掉末尾多余空格
        title = title.Trim();
        content = content.Trim();

        // 7. 根据是否有附件调用不同的发送方法
        if (attachments.Count > 0)
        {
            await arg.Target.Player!.MailManager!.SendMail(sender, title, content, templateId, attachments, expiredDay);
        }
        else
        {
            await arg.Target.Player!.MailManager!.SendMail(sender, title, content, templateId, expiredDay);
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Mail.MailSent"));
    }
}