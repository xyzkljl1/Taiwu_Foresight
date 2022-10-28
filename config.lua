return {
	Author = "xyzkljl1",
	Cover = "cover.jpg",
	Version = 9,
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
			DefaultValue = false
		},

	},
	Source = 1,
	Description = [[
在部分对话选项上(左侧问号处)添加额外提示
代码https://github.com/xyzkljl1/Taiwu_Foresight
mod版本v0.9,用于正式版游戏v0.0.39,不兼容测试分支 

说明：
1.开启"显示OptionKey"时，所有对话选项均会激活左侧问号，并且显示该选项的OptionKey和EventGUID
不开启"显示OptionKey"时，只有被该mod修改了提示的选项，才会显示其OptionKey和EventGUID
GUID/OptionKey是用来区分事件/选项的唯一标识，文本相同的选项也可能是完全不同的分支！
报bug或者希望添加某个选项的提示时，请尽量带上OptionKey

2.如果提示窗的标题上带有"远见"字样，则说明当前的提示是被该mod修改过的，否则则不是
本来就带问号的选项，额外提示会接在原有提示的后面
本来不带问号的选项，额外提示会使问号激活

3.
写着"都一样"的选项，表示当前的对话不管选哪个都不会影响走向，这不代表奇遇/事件链没有分支
只有一个选项的对话，也不代表没有分支
没有发生任何对话，也不代表没有分支
事件的走向可能会受一些隐式判定影响

3.5.成功率部分经常出现XX*99.0,是因为游戏的bug
各个天材地宝奇遇最终获得材料的概率公式不同，也是游戏的bug

4.由于选项众多，难以一一测试
该MOD提供的信息仅供参考，请不要过于信任！！
如果发现或怀疑提示信息有误，请报bug



目前添加了额外提示的选项包括:
1.人物对话:
亲近-背恩绝情

2.外道巢穴:
恶丐窝-道中给与金钱/食物选项、道中废话、终点选项
贼人营寨-拐点、道中捡钱、各种机关、战前震慑、终点战前、终点选项
恶人谷-拐点、终点征服/摧毁巢穴选项
叛徒结伙-道中战胜后选项、终点和叛徒谈判/和门徒汇合/被门徒狂喷选项
悍匪寨-起点给钱和给钱后、拐点1、终点征服/摧毁巢穴选项
乱葬岗-起点、气脉水口明堂三选一、毒气二选一、破阵机关、摧毁/征服选项
迷香阵-起点分支选择、道中小仙女、终点战前、终点选项
弃世绝境-道中围观选项,终点战斗前后选项
邪人死地-起点,破解选项,终点选项
群魔乱舞-终点选项，废话选项
修罗场-起点分支，废话选项，终点选项
异士居-起点分支，终点选项

3.其它巢穴
义士堂-终点选项

4.天材地宝
木材、金铁、织物、玉石、毒物、食材-道中和终点选项]],
	Title = "远见"
}