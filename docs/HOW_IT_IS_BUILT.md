# How DAORework is built, and why I use AI to build it


**About me:** 
Hello! My name is Amber :) Right now I am working as a Junior Dev and lately I have been getting more into my own projects. This has been brewing for a while, but with BioWare's shutdown I felt the need to start dedicating more time on the project to get it released. The Dragon Age series have been a beloved one to me, like many others, and DAO especially a game I will always pivot back to. I've never beat a game more than this one - and that says a lot since I never finish games anymore lol. When I am not gaming or doing coding on the side, I like to watch TV (Outlander is my latest obsession), but I also have gotten pretty decent at crocheting. It's those damn visual patterns that seem so far away! (iykyk)
 More on [LinkedIn](https://www.linkedin.com/in/amber-rastella-14240b302).

## The short version, cutting the crap so they say

I use an AI coding assistant (Anthropic's Claude) to write much of DAORework's code. I, however, do not *vibe code*. That requires a level of ignorance that would be insulting to my bachelor's degree and *checks invisible watch* about to be masters degree. Although AI can generate code, it does not suddenly instill clean code standards, proper design decisions, appropriate architecture into the code itself without someone actually guiding standards and decisions. It may be able to get something working without it; but I promise you when you open the back it's a miracle the thing got past the first Program.cs file (or the like) since it probably STARTS AT 15,000 LINES OF CODE (AKA a 'god file'). 
I assure this is not what is happening here. Although there may need to be fixes or normal debugging, there is unit and regression testing going on. Established engineering standards. The such. Yada yada this was supposed to be the short version.

## The concern is fair

Look, I get it. I just want to start there. You can't come to me with a concern that likely isn't to some degree super valid, especially with the way it probably makes you feel, and there are real climate consequences without any true regulation.
What I cannot ignore is the power that it puts back into ordinary hands. AI has infested the corporate world, and although we hope that would give people their extra 10 or 15 minutes back in their day, there are some who seem to think it is more important to fill the space with work. This is not how AI should be brought to us; these programs being built should offer ease with nothing expected in return. That's why this program will never be charged, EVER. It will always be free and I will continue to update this particular mod manager until the day I die. If someone chooses to take it and do something more, it is on them as I have at least offered a foundation for the next steps. I intend to make Dragon Age games accessible for as long as possible in the hopes it will continue to bring joy to old and newcomers alike.

## What I bring, and what the AI does

I have the knowledge to ask for specific standards, to recognize when something is wrong, and to point it out. I know
when a program needs real design patterns, and which ones, and I make the assistant use them. Its work gets reviewed
against those standards, and what the review finds becomes context for the next step. The AI types much of the code;
it does not decide what DAORework is or what risks it may take.

Every one of these is public in this repository:

- **The rules come first.** [docs/ENGINEERING_STANDARDS.md](ENGINEERING_STANDARDS.md) and [docs/SDD.md](SDD.md) set
  the architecture and hard limits: layers that may not reach into each other, no file over 300 lines, no method over
  50, errors returned instead of hidden. Automated architecture tests fail any change that breaks them, whoever wrote
  it.
- **Decisions are written down before code.** Each design choice has an ADR in [docs/adr](adr) with the options that
  were turned down and why. Where the choice was mine, it says so: for example the 4 GB patch approach and write access
  in [ADR-0013](adr/0013-managed-fixes-in-the-install-folder.md), and in
  [ADR-0018](adr/0018-mod-safety.md) that a mod your antivirus flags is refused with no override, and that programs
  inside a mod are never installed.
- **The plan is phased, with gates.** [docs/ROADMAP.md](ROADMAP.md) lists every task and what has to be true before a
  phase counts as done.
- **Your files are protected by design, and that is tested.** Nothing is written without a plan you see first and a
  backup you can roll back to. A fault-injection suite kills DAORework at every single write and checks your folders
  always end up fully before or fully after the change, never half-changed.
- **I test on a real install.** Each release is run on my own copy of the game, step by step, and the results are
  recorded in [docs/testing](testing).
- **Other modders' work is respected.** DAORework recognizes community fixes and mods so it can help you manage them;
  it never includes or redistributes them ([docs/LEGAL_BOUNDARIES.md](LEGAL_BOUNDARIES.md)).

## Why this matters now

BioWare has closed: EA announced it on 21 October 2026 ([NME](https://www.nme.com/news/mass-effect-sequel-cancelled-bioware-shut-down-lay-offs-3973846),
[Dexerto](https://www.dexerto.com/gaming/ea-shuts-down-bioware-and-cancels-next-mass-effect-game-amid-400-layoffs-3420855/)).
Keeping Dragon Age: Origins playable for everyone and anyone matters more than ever. That means modernizing how it is
set up on today's computers, so people who are not technical can still get it running and enjoy the hard work other
modders have put into this game since 2009. Built entirely by hand, a project this size would take one person far
longer; with these tools, it can exist now, while it still matters. We should use AI to further ourselves, and to keep up in the
game we call capitalism, rather than leave it only to the companies that close studios.

## What the research says about AI and programming

The evidence is mixed, and I would rather show you that than cherry-pick:

- Across three field experiments with 4,867 developers at Microsoft, Accenture and a Fortune 100 company, developers
  with an AI coding assistant completed 26% more tasks, with larger gains for less experienced developers
  ([Cui et al., *Management Science*](https://pubsonline.informs.org/doi/fpi/10.1287/mnsc.2025.00535)).
- In a controlled experiment, developers with GitHub Copilot finished a programming task 55.8% faster than those
  without it ([Peng et al., 2023](https://arxiv.org/abs/2302.06590)).
- But a 2025 randomized trial by METR found experienced open-source developers were *slower* with AI on their own
  projects, while believing they were faster ([InfoQ summary](https://www.infoq.com/news/2025/07/ai-productivity)).

My reading: AI does not make software good by itself. Standards, review and testing do. That is why this project leans
on them so hard.

## What about water?

Water use is a real question, and the honest answer is that it depends heavily on where and how. Some numbers for
scale, all for the United States:

| Use | Water | Source |
|---|---|---|
| One median Gemini text prompt | about 0.26 mL (a few drops), cooling water only | [Google, 2025](https://arxiv.org/abs/2508.15734) (not yet peer reviewed) |
| All data centers (AI and everything else), 2023 | about 17 billion gallons for cooling, plus about 211 billion through the electricity they use | Lawrence Berkeley National Laboratory, as reported in [The Conversation](https://aihub.org/2025/09/24/data-centers-consume-massive-amounts-of-water-companies-rarely-tell-the-public-exactly-how-much) |
| Golf course irrigation, 2024 | about 1.63 million acre-feet, roughly 530 billion gallons a year | [GCSAA survey](https://www.gcsaa.org/who-we-are/media/news/2025/12/30/golf-courses-reduce-water-usage-by-31-percent-according-to-national-survey) |
| Irrigation, 2015 | about 118 billion gallons **per day** | [USGS Circular 1441](https://pubs.usgs.gov/publication/cir1441) |

Nationally, all data centers together use less water than golf courses, and a tiny fraction of what irrigation does.
The fair caveats: data centers are concentrated in some places, so their local effect in dry regions can be large;
their use is growing (Berkeley Lab projects it could double or quadruple by 2028); and companies rarely publish full
figures. DAORework itself uses none of it: it runs entirely on your computer and talks to no AI service.

## Questions

If something here does not hold up, open an issue on GitHub. I would rather be corrected than be wrong in public.
