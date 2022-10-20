return {
	Author = "xyzkljl1",
	Cover = "cover.jpg",
	Version = "1.0.0",
	FrontendPlugins = 
	{
		[1] = "Taiwu_foresight.dll"
	},
	BackendPlugins=
	{
		[1]="Taiwu_foresight_backend.dll"
	},
	FileId=2875708969,
	DefaultSettings = 
	{
		[1] = 
		{
			SettingType = "Toggle",
			Key = "On",
			Description = "开启",
			DisplayName = "开启",
			DefaultValue = true
		},
		[2] = 
		{
			SettingType = "Toggle",
			Key = "ShowOptionKey",
			Description = "需要报Bug或提建议时开启",
			DisplayName = "显示OptionKey",
			DefaultValue = true
		},

	},
	Source = 1,
	Description = [[
内测版！！！
不建议订阅
当然你非要订阅也可以，反馈点bug更好
代码https://github.com/xyzkljl1/Taiwu_Foresight


在部分对话选项上(左侧问号处)添加额外提示
mod版本v1.0.0,用于正式版游戏v0.0.32,不兼容测试分支 (运气好时测试分支也能用，但该mod不会特意做兼容)

说明：
1.开启"显示OptionKey"时，所有对话选项均会激活左侧问号，并且显示该选项的OptionKey和EventGUID
不开启"显示OptionKey"时，只有被该mod修改了提示的选项，才会显示其OptionKey和EventGUID
GUID/OptionKey是用来区分事件/选项的唯一标识，文本相同的选项也可能是完全不同的分支！
报bug或者希望添加某个选项的提示时，请尽量带上OptionKey

2.如果提示窗的标题上带有"远见"字样，则说明当前的提示是被该mod修改过的，否则则不是
本来就带问号的选项，额外提示会接在原有提示的后面
本来不带问号的选项，额外提示会使问号激活

3.写着"都一样"的选项，表示当前的对话不管选哪个都不会影响走向
这不代表奇遇/事件链没有分支，可能你已经进入某个分支无法更改了

4.由于选项众多，难以一一测试
该MOD提供的信息仅供参考，请不要过于信任！！
如果发现或怀疑提示信息有误，请报bug


目前添加了额外提示的选项包括:
1.人物对话:
亲近-背恩绝情

2.外道巢穴:
恶人谷-拐点、终点摧毁巢穴选项
叛徒结伙-道中战胜后选项、终点和叛徒谈判/和门徒汇合/被门徒狂喷选项
悍匪寨-起点给钱和给钱后、拐点1、终点征服/摧毁巢穴选项

]],
	Title = "远见-内测版"
}