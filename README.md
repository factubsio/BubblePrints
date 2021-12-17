
If you want dark mode set the environment variable `BubbleprintsTheme=dark`

FOLLOWING LINKS:

To follow links (denoted by the exclamation point prefixing them), click the link, and then click "follow link". You can also follow links just by clicking on them by setting "Follow link on click" in the settings to "true". 

SEARCHING:

`glitterbuff` will search all blueprints by name and do a fuzzy match, then return the matches in the "best" order. It doesn't get it right 100% of the time, but what you're looking for should be in the top few results.

You can use up/down + enter to navigage the results list, so typically you will do "type search thing, press enter, hope it got it right".

You can also search by type/guid/namespace:

 * t: Type
 * g: Guid
 * n: Namespace

example:

`glitter t:Buff` will fuzzy match by name on `glitter`, and restrict the results to only blueprints whose type matches `Buff`

`g:abcdef` will find all results that have the exact substring `abcdef` in their guid.

![image](https://user-images.githubusercontent.com/65080026/140194615-03c8a91a-f244-4f75-a533-e0df8f3c5fe4.png)
