Installation, auto-update and addon preferences
hello and welcome to this 2024 update for simple bake my intention in this
video is to go over all the features of simple bake one by one nice and slowly
just explain what everything does the reason for that is that simple BS obviously moved on quite a lot since I
did the last one of these videos I always have an intention of making a new one but I can never seem to find the time so I thought I'd take this time to
sit down and just go over every feature every option try and explain what all of them does how you would use them so you
can get a view as to how you might use simple bake in your workflow uh or for whatever you're doing so I'm not going
to use any complicated models I'm just going to use nice simple cubes with materials which will hopefully make it a
lot easier to follow what I'm doing um I'll try and go through everything
without leaving anything out inevitably I will leave something out but I will try not to we'll start right at the
beginning then with installing it so I've got a a pretty stock copy of blender 4.2 installed uh simple bre is
not here not yet so in blender 4.2 onwards uh blender introduced this
concept of extensions which are I think going to gradually replace add-ons for
now simple bake is what's being termed a legacy add-on which isn't insulting at
all so what you have to do is you have to install it manually from dis if you
you have used simple B before you know that it has uh an updating mechanism of its own that's because I don't um
sometimes I published quite a lot of new versions of simple bacon rapid succession and having to go to blend the market both for me to upload each
individual one and for you to download each individual one is a bit of too much
of a pain really so a number of years ago I wrote a script that basically pulls the latest version from my website
and installs it and generally people quite like that I think cuz you I always
want you to be using the latest version of simple bag there is a facility that I'll talk about where you can roll back
to the latest stable version of simple bit just in case a bug has crept into the newest one and it stopping you doing
your work but generally I think you should always be using the the latest version if you can so what I'll do now
is I'll show you how to install it on blender 4.2 so you're going to edit preferences add-ons not extensions
because again it's not an extension yet I will convert simple bake into an extension in the future I think blender
Market have said they've got plans to implement a repository so the big thing about extensions is that they they're
kind of always pulling the latest version online basically a professional version of what I've been doing with simple bake for years but um they
haven't done that yet with blender market so when they do I'll probably probably when they do rather I think
I'll I'll convert simple bake into an extension and maybe use blend the markets repository I just want to see what it looks like first but for now
it's an add-on so what you need to do is you need to go to the add-ons here you need to go to this little button up here
install from disk and then what you'll have done is you'll have downloaded the
zip file available from blender Market what I'm going to do is go to this this directory where I have all the old
versions of simple B and I'm going to deliberately install an older version so the latest version is 1.4.2 I'm going to
deliberately install 1.4.0 the reason for that is that again because it's a slight pain to constantly
upload to blender Market the version on blender Market I only update now and again I kind of just rely on you being
able to download a version there aut to update to the newest version and Away you go so to simulate that I'm going to
deliberately install let's say simple bake 1.4.0 so there's a zip file like you would have downloaded from blender
Market you click it install from disk I've got a little notification down here saying modules installed and you see it
appears in the list now and then if we go over here you can see simple bake is installed so newer version of simple
bake available please update automatically in add-on preferences and this is where you'll use you'll go back
to add-on preferences and you'll use that automatic update so anytime there's an update for simple bake if you come
into the add-ons and go into the preferences like I've just done here big red update button click that simple bake
update complete please restart blender so I'll just do that quickly I'll kill blender bring it
back and there we go simple is up to dat version 1.4.2 installed and ready to
go okay so the next thing I want to discuss is that preferences screen and the options that are there because I
think sometimes people miss these so if I go back to add-ons search for simple bake and then I bring this up you've got
a number of different buttons so because we're at the latest version that uh big red update button isn't there you can
see the release notes which will launch your browser um and you can get a feel
for just kind of how often simple bake is updated here I tried to put a little uh probably less than
professional sounding comment as to what exactly I I'm doing when I upload a new version so you can decide whether it's
worth immediately updating or or leaving it for a bit uh you've got this option here to roll back to the previous
version to be honest I should really say previous stable version um I kind of
update the rollback version when I feel that it's been out for a while people have been using it no one's repoed any B
looks it's pretty it feels pretty stable that's when I make that the new stable version but in honesty I'm trying to be
book free with the latest version in theory It's just sometimes as I say if I publish a new version and there's some
bug I've not spotted you can always go back to the version that was last Dem stable uh there's a support page which
you again we'll just launch your browser to a basic support page I think this now yeah this takes you to this common
baking issues page this is really helpful uh well people tell me this is really help ful uh it sort of talks
about the various things you can kind of run into particularly for people who aren't familiar with baking at all let
alone with simple bake or perhaps they aren't familiar with um blender there's
some warnings on here about blender bugs that still exist years years after being
reported um which boggles the mind but anyway so you've got a lot of kind of
tips there there's a Discord page which is uh where we kind of have a community
of people sometimes that can help each each other particularly useful for things like people who want to use game engines I have to put my hands up and
confess I have zero experience using game engines I really have never developed a game or used Unity or unreal
I just kind of do what people tell me they need simple bake to do so if you need actual help with game engines there
are some people on there I think who probably know well do know a lot more than me this is where you enter your
sketch Fab API key so simple B does have a option to upload to sketch Fab I'm not
sure if people use that anymore it was kind of a defining feature of simple bake when it when I first brought it out
um but I don't know people really anyway if you were to use it you put your API
key in here and then when you've created a an object to a simple bake you you have the option to upload it I'll try
and remember to cover that later in the video um yeah so here is where you have
total control over how your bake textures are named so I do know that certain game engines and certain Studios
have their own rules about how things are named this allows you to change how all the textures get named when they're
created so you've got some variables here to play with object name a batch name which is just a a name you want to
give a batch of bakes which you can set over here on the panel under other settings oops lost my window now um bake
mode which is the the mode in which you're baking bake type uh so sorry bake mode would be PBR or Cycles which we'll
get into in a minute bake type is the type of map you're baking so diffuse submission metalness whatever resolution
obviously the resolution frame uh you can bake a number of different frames in simple bake if you've got animated
textures so that will include the frame number uh it actually gets added anyway automatically if you uh turn on that
option because if it didn't your textures would all have the same name and overwrite each other but this allows
you to control exactly where in the file name it's placed so you can put it at the begin put it at the end Etc
abbreviate resolution uh I try to do tool tips in blend in simple bit rather to try and give you as much information
as possible without having to look things up so always hover over see if the tool tip answers your question but
basically that's does what it says it's going to abbreviate resolutions like 2048 by 2048 to 2K Etc button to restore
it to the default it is the default so it doesn't do anything but you know just takes it back to that that beginning
here here is where you control the aliases so again I know different Studios and who use simple bake and also
different um game engines require these things to be called different things so
diffuse can sometimes be called Al albo is it uh metalness might just be called
metal instead of metalness Etc so you can change the names of how simple bake
names everything to do with those here here is where you set the color space so
in general things like a metalness map and the subs subsurface scattering map
would be non-color uh only really diffuse and emission which are RGB Maps
would be a different color space but you can choose any of the ones available in blender 4.0 uh for whatever your whatever you're
up to same again for special Maps so simple bake as we'll come on to differentiates between the basic kind of
PBR maps and then it also bakes some ones that aren't sort of included in
what you would think of as PBR like ambient occlusion curvature thickness Etc and it's the same options for those
name color space and a button to restore all that to the defaults and then you've
got these kind of tweaks and miscellaneous options so if you don't want simple bit to check it at the current version when you start blender
you check that and it won't do it won't access the internet um there's an option here to unpack no groups no groups are
the bane of my life with simple bake um for reason for technical reasons I have
to kind of it's sort of a really convoluted way I have to unpack node groups to be able to uh work out what
channels need to be baking so simple bait will try and well not try and
simple bait will only unpack the node groups that actually matter so uh it's
hard to describe but basically if you're running into problems it this option someone requested will unpack every node
group it should never really be necessary but you know it's there if you need it um again then simple bake will
only bake PBR from materials that have nodes that are going to give you a PBR result so if you're using a diffuse bsdf
or a glossy bsdf you can't really get PBR from those those aren't physically based shaders um and so simple bit will
if it detects one of those nodes that they can't use in your material it will halt the bake and it won't let you
proceed however sometimes if you buy kind of materials from blender Market or
somewhere else they will be really convol Ed and they might have some of those nodes in there but they're not
doing something or they're being converted back to RGB and basically it just Trips Trips up simple bit because
it it thinks it can't work this will skip the check I don't recommend it because the whole reason that checks
there is because if you bake with noes that can't give PBR results you're going to get nonsense you you know you're
going to get color you're going to get material so you're going to get textures that don't look right uh it could even
crash because again simple B isn't expecting those nodes but for at least a couple of users this is come in clutch
because it allows them to bake with materials that in honesty should be
designed slightly better but it it just skipping that check means the bait can go ahead and in those cases it doesn't
it actually works out fine because the material is just built that way uh
simple bers Channel packing there's two methods for it neither of which are perfect I'm working on a third method
which I hope will be perfect but blend is quite Limited in what it offers you
uh so one the first version and the version simple bit used for years was to channel pack via the compositor so it it
uses compositor nodes to create channels and that mostly Works uh the new method
I is more complicated using um I won't go into it but like python has a library
called numpy uh numi I don't know how you pronounce it and it it's kind of using
maths to to fiddle with all pixels and again that is probably better but it's
still not perfect in my opinion I'm working on a third method that hopefully
will come eventually that will be perfect but for now you can kind of switch between the new version a new
method and the old method and see what what works best for you depending on what you're doing and where you're using
those textures um disable auto smooth for custom split normals so this doesn't
really apply anymore cuz blender I might actually disable that option of it if we're running on blender 4.2 because
blender 4.2 doesn't have Auto smooth anymore so um that's you can just ignore that
really uh all PBR materials are baked at two samples the reason is because behind
the scenes simple bake is running them through an emission Shader so sample count doesn't matter I mean in theory it
could be one sample and that should be sufficient there's no lighting there's no Shadows there's nothing to really
cause noise the exception to that is um sometimes you can use nodes within your
materials for like um if you use a bevel node as a good example a be bevel bevel
node uh it can cause normal maps to look noisy so simple bake deals with that
automatically but if you want real kind of peace of mind you can just tell it
you can override this default behavior of baking so by default it will bake everything at two samples unless it
detects a reason in the material to up it and then it will up it to a 100 samples but here if you want the real
peace of mind you could just put everything at 100 samples it'll take slightly longer to bake everything but
um you know it will guarantee no noise even if you've got those those weird occasions where you're using a node that
it might introduce it you can change the location of blend of simple bake rather so you can display it on the render
panel or the mend panel as I've said in this red warning I think it crashes all the time when you have it on the mend
panel I don't know why it shouldn't I'm looking for a reason why but I can't
find one I actually like it on the M panel better but it just crashed blender seems to crash way more often I I don't
know why it does but so the option is there I don't recommend it until I found out why the damn thing crashes all the
time uh and then this option is toggles and tick boxes so for years for years of
simple bake it was just tick boxes because I never really bothered with what it looked like I was more
interested in how it worked um some people love the tick boxes I think I don't I don't know I but I gave the
option to change it to uh toggles in in an attempt to make simple bait look a
bit more bit more professional but yeah to each their own I guess you've got the
option there to do whatever you want and I think that's everything for the add-on
PBR Bake vs CyclesBake
preferences okay let's talk about the first kind of choice you're faced with
PBR bake or Cycles bake I'll keep this one real simple PBR bake is baking PBR Maps so these
record the properties of the material so how much diffuse it has how much metalness it has how much normal it h
soorry the normal map that goes with it to show the normals of the material how much transmission it has Etc no lighting
no Shadows you're getting kind of the instructions for blender or for Unity or
un real or whatever you know sketch Fab you you B you're creating the instructions for how to reproduce how
that material looks that's how I'd encourage you to think about it that's how I think about it Cycles bake is the
kind of traditional baking mode of blender you are baking the appearance of the material so you're you're baking the
appearance of the surface including the lights any Shadows any Reflections
they're all kind of baked in as if you've taken a photograph of the surface that's different as I say to PBR
where you're recording the properties and then wherever you're taking your textures sketch for album real Unity
that's going to calculate the shadows and the reflections and all that stuff
whereas Cycles bake again which is basically my name for the traditional baking modes in blender is baking them
into it as if you've taken a picture hopefully that makes sense that's how I think about it uh those are the two kind
of fundamental modes in which simple bake operates
Settings presets
okay so we'll start with a uh section of the panel that's available in both PBR
bake and Cycles bake and that settings presets so there's two types of presets
in simple bake uh sorry by pre should say by preset I mean recording all your settings so you know what objects you've
got in your bake objects list what Maps you've got turned on what I don't know what Ray distance you've got for baking
to typ you everything gets written to uh the preset you've got two types of presets one Global and that's stored
outside of your blend file in the file system and one uh blend file presets
which as you're going to guess is stored in the blend file and is specific to that blend file so if you open any blend
file you'll always see all your Global presets but the blend file presets are specific to that blend file and all you
do is you set your options on the panel as you want them you give this a name
and then you click the little save icon and it saves it in the list and that again that's created in the file system
you can see there it's in home leis far well I'm on Linux and I'm using a flatback version so that that directory
structure is a bit convoluted but basically blender config uh it's One Directory up from
where simple bake itself lives which seems to be where other add-ons store their data so I thought I'd shamelessly
copy them so yeah to load a preset just select it hit the little tick icon to
delete preset hit the little cross icon it will refresh automatically but you can't manually refresh it there um as I
say presets include the bake objects in your bake list and obviously from fi for
Global presets from file to file those objects might not exist but what simple bait will do is it will try to load them
if they are there so it won't be a problem if they're not there but also you can also turn this off so it'll load
every setting but the bake objects will stay the same so if I demonstrate that by saying I
don't know let's let's remove it so we've actually got an empty bake objects list let's turn this off and load it see
it's loaded all the settings but it hasn't put the cube back in the bake objects list hopefully that makes sense
I'm not going to go over this because it's exactly the same buttons you obviously don't need this option for the blend file presets uh but it's the same
concept and those are saved with your blend
The Bake Objects List
file okay so the next thing we're going to talk about is also available in both PBR bake and Cycles bake and that's the
bake objects section of the panel this unless you enable bake to Target is really simple you just add the
objects you want to bake to the list it's smart enough that it won't add um
anything other than mesh objects but any mesh objects you want to bake go on the list you can move them up and down in
honesty there's not a lot of point to that it just changes the order which they're baked and maybe just for
convenience sake for you looking at the list if you've got a long list you might want to position certain ones at the top
so some ones at the bottom but in honesty it doesn't make that much of a difference you can clear the list you can refresh the list the reason for that
is that if you say add an object maybe remove it from your scene simple bait doesn't know it's gone now it will
refresh the list itself when you try to bake so it'll never try to do anything that's impossible but um if you want to
trigger a manual refresh there's a button right there for it I'm going to come back to bake to Target and isolate
objects because I think it's probably better to talk about those after we've discussed some of the other
PBR Baking
options so moving on staying in PBR mode I'll go through everything in PBR mode
and then I'll go to Cycles bake mode and try and just cover the bits that are different so PBR bakes uh selection
panel is obviously specific to PBR shockingly uh here you select the
different types of map that you want to bake uh you can select all you can select none a new button here exists
that will try to guess the maps that your uh object is using so let's just
show how that works I'm going to add this Cube cube. 002 and I'm going to
click this and it's picking up nothing the reason it's picking up nothing is because I've changed
nothing from the um default settings of a principal
bsdf if I'm going to plug something into it um what am I doing yeah if I'm going
to just plug something into it it will detect that and it knows that I've changed that and therefore it's
highlighting that it's it's selecting diffuse for your bake similarly if I change the default settings let's push
the roughness up hit the button again it's detecting it so this may seem a bit
pointless if you're creating your own material but if you've actually let's say purchased material I had someone on
Discord with this problem and it's maybe it's a a bunch of different um Shader
nodes mixed together maybe they're hidden inside node groups and you know
maybe some of them have input some of them don't I think it can become my blender's Frozen now cuz it's it's
trying to uh recalculate the Shader but um it can become really complicated to
actually work out what your material is using so that's why that that button
exists um and again it's it see it's detected here just the ones you've
changed so even if you've got a node group like this where all the principal bsdf sockets are plugged into the node
group it should still detect them so I'll just push transmission weight up a bit and yeah it's detect Ed the
transmission has been changed so handy little button there and
again it will help I think a lot with um the case where you like say if you've
purchased some node groups or something like that or sorry purchase some materials and it's full of node groups
and you don't quite know what channels are being used that should automatically automatically detect
it so I'm just going to reset settings on this file so don't confuse us uh
there's a few extra options that appear uh you can see when I've selected the fuse I now have this option but it's
gray out and the tip explains why extra options are not available because you are not exporting your bakes so certain
things require I mean the technical reason is I can only do certain things if the file exists outside of blender uh
because again blender is quite Limited in what you can do internally once it's outside of blender I can do fancier
stuff especially with udims udims are another pain in the ass when it comes to
the simple way because blender kind of handles udm and kind of doesn't I think
every version of blender it gets a bit better but it's still kind of not there
with udims uh in my opinion it it you can't kind of access the data properly
from a from a scripting point of view so certain things have to go outside of blender for them to work so I'll just
turn that on quickly we'll come back and talk about this obviously and now I've got access to this option so I can have
diffuse or I can have diffuse with AO and again skipping ahead slightly you'll see that it's turned on AO and I can't
turn it off because I need it for this option there's a couple of other options so if I turn on normal
map uh it's going to give me a choice between an open and direct text normal map blender uses
openg uh but certain game engines are expecting direct Tex it's just a different way of formatting normal Maps
so you can choose between them similarly if I turn on roughness I get the option to to switch between rough and glossy um
these are kind of an invert of each other but again certain applications game engines sketch Fab whatever they
might be expecting roughness they might be expecting glossy blender deals in roughness which you see from the
uh principal bsdf blender deals in roughness but a glossy is an inverted
version of a roughness map so just saves a bit of time there I think that's all of them just trying to remind myself
yeah those are the three kind of extra options depend on what you've got selected if you're wondering what this button is this is actually for
transmission roughness it still exists because simple bik is still compatible with oldo versions of blender but there
is no transmission roughness in in blender 4.0 and above I think so that's
why that that doesn't exist so I think that's everything um I'll just show
quickly what I mean about maybe a new file um I'll just show quickly what I mean about the
so if I create a file here and this is not professional but if I
just create a bit of a feature in it there um so if I just bake the
diffuse it will all just be gray so I'll go here to look at the output yeah just
gray uh if I mix it with AO uh okay so again I've got to save my
file so that I can and export my bakes so I will mix that with
AO 50% let's say and we
bake I get my ambient occlusion I get my diffuse you can see
this time it's been multiplied with the AO so I've actually got the ambient occlusion map on its own because I
needed that but my diffuse map has AO mixed into it
and that's that's about it for that section of the panel so we'll move on next section of the panel we'll talk
Special Bakes
about is these special bakes so I couldn't think of a better name for these when I first started making simple
bake I was going to go with like a a baking theme and like specials in a
restaurant I can't remember it was UT stupid but anyway the name has stayed so
the these are types of bake that um are on like um on on your standard sort of
PBR so curvature ambient exclusion light map thickness vertex color if you don't
know what these are you're probably best Googling them um I won't go into the
details of of kind of what the different ones are the only thing I will say you see I can't deselect ambient occlusion
because we've told it we want diffus and AO the other thing I will say is that these Maps ambient exclusion curvature
thickness um I think light map yeah think like map
these are using simple bake's own materials so when you bake these Maps simple bake will import a material that
does that it creates this effect and bake that material now if you are very picky about
what these things look like you can change those materials so there's a button here import specials materials
for editing so if I click that and then I go for example let's just create a
dummy object for a second and then if I put a material on
it you see I've got the simple B ambient occlusion map curvature map light map map and thickness map oh sorry materials
what am I talking about amb so those are materials so if I go over to
shading um yeah so I can then edit them if I want and you'll see they're not
that complicated so the ambient occlusion is literally just an AO node curat is a bit more
complicated uh but still not really complicated thickness is quite complicated and
um which one of them we done light map is just is literally just a diffuse with the so you can capture the light cap
falling on the object but if you edit those and save your blend file they will continue to apply for that blend file so
those materials now exist in my blend file simple but won't import them again it will use the ones that are already
there and that's why that button exists um so color ID I'll just briefly
mention it it will create a random color for each material on your object so if you've ever used substance painter you
know that it relies or can rely on like color ID Maps so that you can mask
certain things and paint only on certain areas if you bake a color ID map with simple bake that's exactly what it'll do
so if I was to change one of these materials let's
give this object sorry so I'll give this face is the only
one selected I'll assign that new material and then in if we bake this
object color ID map see it gives you see in the bottom
left there it gives each material a different color so that's what color I Doos vertex color is vertex colors again
if you don't know what that is it's it's a way of painting the vertices you're probably best Googling it um just
looking at some of the extra options that become available so uh curvature doesn't have any apart from to import
the material that it uses like we've already talked about thickness has uh as you'll have seen when we look to the
material there's an AO node in there and if you've got an AO node sample count becomes relevant so you can set the
sample count to more or less if you if you want better results ambient collusion similarly has that sample
count uh light map really needs a sample count because
as you saw light map is just a diffuse node light map is actually pretty much just Cycles bake the uh the traditional
way of baking in blender it's taking into account uh Shadow and and and lighting obviously because that's the
whole point of why it exists so then you really do need a sample count and that's just the the same kind of sample count
you use when rendering in cycle spake or just rendering a scene uh through the camera as you would normally uh you have
this option here to apply Color management to your light map so when the light map gets saved
externally uh it can either ignore the color management or it can apply the
color management where is it these days can't remember blender has a
section somewhere for uh column management I'm struggling to find oh there you go down there so it's
in the render panel so you can either um apply that color management to the exported texture or you can send it out
without the color management so there's two options there then we get into D noise um so D noise again if you don't
quite understand the concept at all you can Google it but effectively when you're talking about textures like light
map remember I said light map is basically just like baking Cycles bake or baking oh sorry or rendering
generally uh you get noise because you're trying to bake you're trying to take into
account lighting and lighting and shadows and things causes noise in the texture and the more samples you give it
the less noise you'll get but then you can use a post-processing filter at the end to
kind of cheat and get rid of even more of the noise so you can get away with fewer samples quicker bake time and then
a clever algorithm to tidy it up blender does this automatically which is why
this is kind of selected anyway anything you bake in blender anything you render in blender I think gets run through the
kind of default Cycles den noiser and these are just the default options the same ones you will find up here
somewhere Advanced yeah so I pause the video there
for a second so you didn't have to suffer through me uh stupidly clicking around the panel but my mistake was I
was set to Eevee so um Blended as all kind of baking in cycles and simple bake
switches to cycles and then back to Eevee if you were in Eevee seamlessly behind the scenes but it's always cycles
and Eevee doesn't really have the same kind of Concepts but in in Cycles when you're talking about noise and um when
you're rendering in Cycles you you you are firing Rays at a object and looking for light sources and it gives you noise
and anyway my point is blender has a kind of default option for Den noising and you'll notice those options are
exactly the same as you are offered down on the simple bake panel for the textures you are baking additional option now you
probably don't really want to have these both on at the same time but an additional option is to run your B is to
turn off the defaulty noising and run it through the compositor why might you want to do that because the compositor
has its own um I'll just turn this on a second the compositor has its own d
noise node so it again if you don't know what the compositor is you might Google
it but um why would you want to run it through the the compositor when Cycles
blender has its own defaulty noising and the reason mainly these days is because
if I go back to simple bake support page which is basically a list of issues is things like this bug where
you'd see dark edges along the lines of your UV Islands now this let's check if
it's still exists yeah it's still open it's 3 years old um and that is an issue
with the Cycles denoiser so you can see here I talk about enabling the compositor denoiser instead um to to
perhaps get a results because I don't think that suffers from the same weird bug but um you wouldn't typically have
both turned on but you've got the option for one or or both of them if you need
it tip here explains why sample counts are relevant basically what I rambled on about just a few minutes ago uh and
we've already discussed the input button so I think that's everything on that section of the
Texture Settings
panel so the next thing we're going to talk about is the texture settings section of the panel and this is exactly
what you might think it is you set the texture resolution you want to bake at and you can also set a different output
resolution so why might you want to set a different output resolution the most
common use case is to bake at a high resolution say 4K and then output at a
lower resolution and doing this you get what I call kind of a poor man's
anti-aliasing so there's no real anti-aliasing option in blendo in your baking textures so it's not something I
can kind of just do automatically the only real way to do it uh is to make at a higher resolution and then scale down
and the act of scaling down gives you an anti-aliasing type effect so it'll get
rid of some of those Jagged edges it it will just look better but experiment and
um you know you'll see what's best bake margin refers to how much the island
sorry how much the bake extends beyond the boundaries of the UV Island so if I
just clear all images and let's bake again so we're going to get a diffuse
let's just stick with diffuse on its own so I'll just quickly bake a diffuse map from that uh that Cube we've got there
and if I go over to check that out you can see that the bake extends beyond the
boundaries of my UVS now you don't want it to do that too much because if I
haven't got any here if I had other objects or other Islands from this object then eventually they bleed into
each other and it would be a mess um so you don't want it to be too much but you also don't want it to be too little
because if you are right on the boundary of your uvr Island it can clip it and then when the texture is displayed
wherever you're displaying it in you a game engine or sketch fop or whatever or blender you'll have a little Gap and a
little patch of black where it doesn't where the texture doesn't quite cover the UV Island so simple bait does its
best to give you an appropriate bake margin based on your text resolution you can see that it's changing along there
you may have to experiment in some cases but it does a first guess at what what seems
appropriate uh my margin type you can't really see it here but there are different ways of generating what this
margin looks like so it's either based on extending which I think is where it takes the very last pixel and just be at
the edge of the island and just extends it out for the margin and adjacent faces I think where it does something more
clever where it tries to replicate the appearance I I don't really know what's
better but uh blender offers both options so simple bake offers both
options uh these options control the well this one controls the quality of
the image so when you create a new image in blender you have this option to create it just as default or as a 32-bit
float image a 32-bit float image contains more data so you can see that
option was turned off when I did this bake so this image you can see here srgb
8 A8 RGB B image that's kind of the default if I turn this on and I bake
again
you can see that the image it's created this time is an rgba 16f which is a 16bit float the only well the time this
is really important is here for normal maps and that's why simple bait will
automatically create all normal Maps as 16bit float images uh you can disable
that behavior down in other settings Don't Force this is called Don't Force
height bit depth normal map so if you turn that on it will just treat it like any other texture but by default simple
bait will create normal Maps a 16bit float and then this option will create
everything a 16bit float it shouldn't matter too much for textures that aren't normal Maps but if you really want to
make sure you're capturing all the detail you can turn that on and internally within blender everything
will be created a 32-bit flow you got to keep in mind that if you're exporting your bakes which we haven't come to yet
then that changes the equation again because you can create it as a 16bit floating blender but if you export it as
a JPEG you're going to lose all the detail regardless of what it was created
as internally so this is all internal images 32-bit float keep in mind that your export settings if you are
exporting your bakes are also going to affect your image quality transparent
background is exactly what it sounds like for various reasons oh this by the way this rendering issue I don't know
what causes this but if you just kind of Click these it'll reset but blender
seems to screw up sometimes on it's rendering um just something to look out for one of the fun joys of using blender
uh so that's exactly what it sounds like so if I bake with the transparent background
option turned on you can see that instead of a black background ground it's a transparent
background there's various reasons you might want that but you know it depends on what you're doing with the textures
afterwards multiple objects to one texture set is probably one of the most important options on here so very
briefly I've got my two cubes here so let's add them both let's say we want them both to be baked to the same
texture set and what this means is it's going to take both those cubes um
there's an option to automatically deal with UVS which avoid us having to um
talk about that before we're ready I'm just going to manually create a UV map for both of
them see I've got them both selected in edit mode and I'm going to space them apart so each of them now has a UV map
that if you combine the UV Maps they don't overlap so they still got separate UV Maps but when looking at them
together they don't overlap if that makes sense so now multiple objects to one texture set
I'm going to bake what are we baking just the fuse and it will create what's the
message the fuse in material light map okay so one of the materials I had
applied from one of the previous tests isn't PBR compatible so it's warning me
that this material here uh if you watch the special bake section yet you'll know
that I put a material on that Cube so I could show you but that's not a that's
not a uh PB are um compatible Shader as we talked about in one of the the
earlier bits of this video so simple bake doesn't let me let me bake so I'm going to change that material now let's
get rid of that uh it doesn't really matter what we what we put on here but as long as it's
a PBR material which that is so is that so is that so it should be good to go
let's go back to this view so we can look at the texture that's coming back and and
let's bake and it takes a little longer
because you have to send in blender only lets you send one object to the bake engine at a time but see here it's
created one diffuse it's called merged bake because that I didn't change the default name but you can call it
whatever obviously when you're baking a single object on its own the textures all have the name of the object in there
when you're baking multiple objects you have to give it a name because how does it know which one you want so merge bake
and it's created one diffuse map for both of them that's from the previous bake so ignore that one uh and they
don't overlap because we manually prepared the UVS and it will create one for every every map you've selected so
this is our merge bake bake one PBR diffuse metalness normal transmission
all of them so that's probably one of the most important options cuz that quite often people want to do that they
want to bake multiple objects but just a one set of textures uh if you're not
going to have simple bake deal with the UVS you have to manually make sure they don't overlap like you just saw me
Export Settings
do okay let's keep going um so we've talked about uh texture
settings what I'm going to do is I'm just going to reset all the settings just so we we know where we're we know
what we're doing um so now we're going to talk about export settings so let's
let's start with a simple bake no pun intended I will add just the one object
diffuse and then again with this weird rendering bug which I think is blender
rather than simple bake but if I find a reason for it I will fix it it just seems to not want to render the panel
there we go uh usually clicking off and clicking back on will kind of force it to refresh so I don't want all of them I
just want diffuse uh no special bakes this is the way to use Simple bit by the way just
start at the top panel and then kind of work your way down um no special bakes
texture settings 1024 1024 is fine don't care about any of those options on this
occasion and let's say we're going to export our bakes so the first thing we need to do is specify where we're going
to export them so this button will basically change it to default to be a
directory called simple bake uncore bakes in the same place that your blend file is saved so in this case it's home
Lewis on titles this will just be created in my Linux home directory uh but I can change that I can go in here
and I can change it to whatever I want so I could create it directly in my home directory I could go into my desktop Etc
but let's keep it as the default blender uh I don't know what the word is
the way blender denotes where your blend file is saved is SL slash so for SL
slash means where my blend file file is saved and Then followed by the directory you want your bases to be in if that
makes any sense uh so we won't use these options for now I'm just going to bake this
quickly you see it's still using the UV map I used when I was uh moving the UVS
about and I'm going to quickly bring up my home directory which is absolute mess actually here's the simple bake bakes uh
I've still got bakes from the last time I was messing around with this the one that was cre just now is here and it's
saved here as a as a PNG just like we wanted subfolder per object is when you
are baking more than one objects and I think you can probably guess how this is going to work so I'll turn that on I
will um God that he's really getting annoying isn't it I will bake
again and then let's go back here and you can see that's from the previous bake but it's created these two folders
and puts the Textures in there so this can be quite handy if you're baking a lot of different objects maybe a lot of
different maps and you want them all to be divided up into folders for each object it's just an organizational thing
basically uh export diffuse with color management settings I think we've talked about that uh when we were talking about
where were we talking about that is it under special bakes
oh is when we were we were talking about light maps and it's um this option wasn't apply Color management to the
light map so that's when it's created within blender this is saying um if your diffuse map whether you want to export
it with those color management settings that we already talked about so blenders color management settings you won't want
to do that a lot I don't think but sometimes if you've got your material looking exactly how you want it and some
of that is dependent on the color management settings you're using you might want to export it with color management settings but I mean typically
I I don't use that all export 16 bits so this is where things get even more
confusing remember this option the uh every create everything oh my God create everything
internally with 32 bit so again when I talked about that I was saying that what you create the textures at internally in
blender can be affected by they're exported as when you're exporting if
you're using PNG as your format 16bit is the maximum you can achieve uh if you
switch to exr you can actually it well it doesn't even give you the option because it is just the default uh you
will export at 32bit image files but 16bit is the maximum for um
PNG this option exists because by Def if you turned it off
again going back to what we were discussing earlier only normal Maps so it it explains it in the tool tip normal
maps are always exported at 16bit where this option causes all images to be exported at 16 bit I would recommend
keeping that turned on because 8 bit pngs don't always look great the only
reason it's there as an option to turn on and off is that some people are using
textures that they've baked in applications I think second life is one
of them there's something people have told me about that it it will only accept 8bit textures so they literally
have no choice um so if you if you want to just export things as a as a measly
eight bit you can turn this off and if you really want even your normal apps to
be low quality remember we discussed this option to not force High bit normal
apps down here but for most users most of the time I would say just keep that turned on um and you'll always be
getting the maximum quality you can there's different options for different types of export so jpeg has this quality
slider cuz it's a lossy compression format Tiff is similar to PNG uh where
you've got the option for 16 bit TGA has no options TGA raw again you're talking
about 16 bit or not 16 bit so you you kind of design Philosophy for simple B because you see the options that are
relevant and you don't see the options that aren't so that's why why that's why that exists if you don't know what it
does or if you don't really know what you want recommend keeping it turned on and keep internal images after export is
exactly what it sounds like so you see me bake and then look in the file system
at the uh images that it have has generated but those images also exist inside of blender if you really don't
want them to those images can be taken away um if you're using the images if
you're asking simp Le bake to use the images in blender afterwards so we've not talked about it yet whether it's
this copy objects and apply bakes option you can't disable well you can't if you
disable this you can't enable that because simple bag needs those images in blender to be able to create the
material for your copy objects and apply bites option but if you don't need them
and all you care about is saving the images externally you might not want to keep them internally so that's why
that's there similar to export bakes you can also export the mesh and here it's a
choice between fbx and uh gltf so fbx comes with these options of apply object
modifiers before export or apply transformation uh these are basic options if you really want to get into
it you can choose one of your presets if you don't know how to create a preset it's not a simple wake thing
it's a blender thing but if you're going into like exporting like I am here you
can create a preset with the options that you want
and save it and then once you've created that preset that preset will appear in
this list for you to select and then simple bake will just use that as as as its preset so it will use that as the
settings to export so that's probably the advanced users option if you don't
you don't have to specify a preset it will just export it to fbx what it will export is in fact I'll
show you so let's just take this object
for example um maybe give it something more interesting as
a texture so we can see it oh maybe that object I don't know why I've edited the
wrong object there but okay um oh I was editing the material that was only on one face wasn't I and this one's got it
on all the faces so that's fine uh so we'll take this we will
um let's refresh our list so we've only got one Cube turn this off we only want
diffuse uh no special bakes textures are okay at that uh resolution I'm going to
export bakes and Export mesh and then let's
bake so that's finished and then if I go over to my external file system so that
was from before this is the only object we baked you see I've got an fbx file here and if I import that fbx file let's
start uh save my scene and start a new one and then I'm going
to let's clear everything from this scene and let's just import that
fbx and then it should come if blender's doing its job with a material that
correctly refers to the texture that only exists outside of blender the the texture we
saved externally so you can see that here that it's located in that
directory it is external and yeah it's so the fbx file
what I'm trying to say is the fbx file gets exported with a ignore that ignore my Alexa uh the fbx file gets exported
with a reference to the uh to the texture as saved in this directory here
UV Settings, UDIMs and UV maps generally
with it there we go okay moving on we're going to talk
about probably one of the most complicated bits which is UV Maps so
when you're baking as explained on the uh baking support uh common
questions page so again if you go back to simple bake and click to load the support page there's a whole section
here on it's almost always an issue of your V Maps so many people contact me
about problems with bake and quite often that turns out to be a UV map that just
isn't suitable for baking so for good results when baking you need a nice clean UV map every point on your surface
of your model needs to have a unique pointing UV space so if you remember
before when we were baking those two cubes to one map I went to pains to make
sure that the uh UVS didn't overlap um
I'm not doing it right here yeah didn't overlap because if they did then the
bites would be on top of each other but the same goes if you're just baking one model you need to make sure you've got a
nice clean UV map what I quite often see is someone or some people who have UV
mapped quite a or UV unrapped quite a complicated model and they've got thousands and thousands of tiny little
margin tiny little islands and every one of those islands if you remember when we
talk talks about the bake margin setting every one of those Island will have a margin and there comes a point when
you've just got so many Island uh that no matter how big your
bake textures are so you can put your texture resolution up to 8K 10K you're going to run out of room it's just not
going to work you need a really clean UV map and in honesty the best way to do
that is to manually unwrap your model I mean it's not fun um um there are lots
of tutorials online about how to do that properly but you will always get the best result by manually unwrapping your
model that said simple bit does have a number of options for generating and
manipulating UV Maps so we'll talk about them now so if I select at least one
object for bake it's telling me here that I've not got anything in my bake objects list so let's add one not that
one tell you what let's just just start again with a
nice clean Cube there we go so I'm going to create
a new UV map for that Cube don't really need to because by default blender gives you a good UV map but for the purpos of
illustration I'm going to tell it to create a new UV map for that Cube and
I'm going to bake and there you can see it's no longer that t-shape it's created a new
well the UV map because we have this option in enabled restore original UVS
after the bake simple bake has restored this t-shaped UV map that blender gave
us by default but if you go into the data section and then UV maps you can see that it's actually baked to a UV map
that it's UV unwrapped with smart UV project and all that's done is it's called blender's standard
smart UV project which you will get if you're in the UV editing space and you hit U one of the op options this is what
simple baker has done but it's not bothered to bring up the dialogue for you it's just kind of done it with its own settings and you can set the margin
so each one of these things that's unwrapped is a UV Island unlike the the t-shape that it gives you these are all
it's basically taken every face and made it into an island because that's how smart UV project Works uh this gives you
the spacing between them um and that's that's basically it so if
this didn't have a good UV map sometimes times you can get a really good result from Smart UV project and by just
enabling this you'll get a you know you'll get a great result some most of the time you won't in honesty especially
if it's an organic model again you're better manually unwrapping but if you
want a quick and dirty solution it's there if I was baking more than one object so let's add two objects to my
bake objects list let's say I was using this object multiple objects to one texture set so the first thing to
notice that if I select two objects and I have multiple objects to one texture set turned on simple bake is warning me
first of all it's saying that well this is just for information so by default we're going to detect udm tiles we'll
talk about that in a second but it's also warning me here saying you're baking multiple objects to one texture
set with existing UVS because we've turned off new UVS you need to make sure they manually don't overlap and that's
what you saw me doing earlier in the video where I dragged the two UVS off the the top of each other to show you
how to use the multiple objects to one texture set option um if I do it now
this will just bake two objects on top of each other because both these objects have their UVS in exactly the same
position so you can do a number of different things here so if I enable new UV Maps first thing to know is that
warnings gone away but there are different ways of generating a UV map so
smart UV project individual is the same as the option we saw for one object so
it will just you smart UV project this object then smart UV project this object
and again because they're exactly the same object the UVS will be on top of each other and so you see current
settings will unwrap objects individually but bait to one texture set bites will be on top of each other you don't really ever want to do this it's
just there in case you really really know what you're doing smart UV project Atlas is a better option so what this
will do is Select both objects and it will UV unwrap them and effectively what
you'll get is this so it'll smart you V project both of them but because you're doing them both at once blender is smart
enough to uh make sure the islands don't overlap so that's what that option will
do smart UV projector in Atlas Atlas by the way is is what people call a UV map
that spans multiple objects so again these objects have their own individual UV map you can see this one is this I
don't know seven shape and oops this one is here because that's
just how blenders arrange them and when you put when you view them both at the same
time you would call that like an atlas map because it's the UVS of multiple
objects at the same time so this object will smart UV project our two bake objects into an atlas map this option
will take whatever they've got as their active UVS and it will instead of
creating new UVS it will just rearrange them so they fit so the best way to
illustrate this is actually to get rid of our cubes let's go back to two fresh cubes and these both have the uh the UV
M blender gives them by default this t-shape so if I was to UV unwrap them
we'd get the individual squares again detached from each other but I'm going to have this combined UVS by Atlas
option um on un believably annoying and then
I'm going to B let's add to bake objects to the list so we're actually baking something uh combine UVS by Atlas y yep
yep and you see what it's done now what it hasn't done now because I
didn't turn on the option I wanted so new UV Maps combine by
atlas there we go so so you see how it's taken the two individual UV Maps these
t-shapes and it's actually combined them again because we've got this option enabled to restore where are we restore
original UV maps you can see that it's actually restored the UVS they had before but if I switch them both back to
simple bake you can see that they uh they're two T's and it's rearrange them
but not split the island so it's not created a new UV Islands it's just
rearrange the UV islands that were already there hopefully that makes sense with this arranging of the UV Islands uh
comes a bunch of options and these are really the same options you will see if
you go to the UV editor and pack the existing Islands you see you get these
options about how they're packed so these are the same options you get here so it can scale them rotate them so if
one of them was massive and one of them was smaller and I have this option to to
scale them it would scale them so that they fit and average will actually average them so that one isn't
disproportionately larger than the other so they will they will maintain the same
kind of relevant proportions if again if you see what I mean so you've got all these options and these are all only
appear when you're using this option because that's where you're shuffling UV
shuffling existing UVS about if you're projecting them into a new Atlas you're creating uh new UVS new uvr Islands
completely so you don't you don't get those options available so there's a couple of other options here so correct
aspects um so again that is an option you get when you're UV unwrapping something in blender you're best
Googling it if you don't quite understand what it means but if you are um if smart UV project where is it yeah
correct aspect is one of the options so if if your UVS look skewed it's because the aspect isn't is being corrected when
you don't want it to be so you can you can turn that off and on again you're best Googling that it can Pro someone can probably explain it better than I
can uh move new move new UVS to top that's a tongue twister again you've
seen how simple bait creates a u a new UV map when you use certain
options sometimes for reasons that people have explained to me and I honestly have forgotten it's preferable
to have that new UV map at the top of your list perhaps to work better with other add-ons I I can't remember the
reason why someone wanted that but that's why that option exists because some people I think find that useful and
then the last thing to mention is udims there's no automatic way to correct there's no automatic way to create udims
so what you have is the ability to detect them if you've already created them so udims are a whole Topic in
themselves I think I've said earlier in this video that blender sort of supports them and it's getting better but I don't
think it really does support them properly yet but the way you would use them at the moment is if I go into the
UV editing space I create a new image and I tell it I want tiled I'm not actually going to use this image this is
UDIMs
more just so I can see where I want to position my Islands but I'm going to create one here maybe add a couple of
tiles to it and then I'll position my that's way
too many tiles I don't know why I did that and then I'll position my islands where I want them so
maybe maybe I want uh I don't know maybe these faces have a ton of detail on them
I want them on their own udm and I want them to be massive maybe this one's got loads of detail and I basically want it
to have a tile to itself uh and then maybe these are just kind of average let's go to that last
tile so that's how you kind of set up your udims again this this image I've created isn't actually going to be used
what's going to happen and a simple bakeer is going to detect that you've used udims and it's going to bake to
those udm tiles so you get told about that here udm tiles are detected when UV
so I'm going to turn that off because we've only got one object udm tiles are detected when UV is extending on the standard UV space so the standard UV
space is basically this first tile objects without udm tiles can be baked together so again if you were using this
multiple objects option let's say this Cube had udims this Cube doesn't you can bit them together just fine but you'll
get udm tiles for all of them because it needs it needs three udm tiles for this
Cube so this Cube will get three UD tiles although two of them are going to be blank because it's only really only
exists in that first tile um is anything else to mention don't
think so so I'll show you that quickly so what are we baking we're just baking Cube 3 which is a udm thing the way we
just did it we're not exporting so I'm going to bake it
and there you can see that it's automatically detected the udm tiles that texture I used to just to position
the udm tiles still exist but we don't need it uh that's the wrong one that's
also the wrong one that's the wrong one that's the right one there we go um all
those protectors from previous bakes so they have kind of the udims if you want
to see what that looks like in the external file system we can turn on okay I've not saved my blend file so let's
save my blend file here I'm going to export those bakes again with a stupid rendering
problem um and then I'm going to bring us over
to the output folder so this is the folder in my external file system udims exist externally from blender as these
files this one01 which is the first tile second tile third tile and then another
application blender uh sorry another application like um like a game engine
or sketch well sketch FB doesn't support udm that's a bad example another application like another 3D modeling
program or game engine should recognize this format as as udims and that's
basically how udims work uh hopefully the supporting blender will get better but for now it's that kind of create a
dummy image place the udims there's no automatic way to create edems you've got to do it yourself uh yeah and I think
that's about it actually for the UV Channel maybe it's not uh panel section of the panel maybe it's not as
Prefer existing UV Maps called SimpleBake
complicated as I feared um onwards to the next one so I'm recording this later because
I've just realized there's something I missed when I went over the UV settings and it's this option here prefer
existing UVS called simple bake and I just want to illustrate that really
quickly so I've got an object here and I've got uh a UV map the default one
that blender G me and it's this t-shape what I can do what what you
sometimes do is you have your material and you actually explicitly refer to the UV Maps you want
to use when you're I don't know displaying different kinds of textures
so I don't know something like that maybe
um yeah so you see now this this UV map is dying
driving the appearance of my procedural texture which isn't usually the case
procedural textur is usually used automatically generated coordinates but I've explicitly told it to use that but
this might not be the UV map I want to bake to so if you go back
to this um I keep coming back to it but
this tips page for simple bake one thing I talk about here is that
it's best for good results um a good General tip is to create a
separate UV map for baking you can name that simple bake and use the option prefer existing UV maps called simple
bake and that's why this option exists because you might have you your object might have loads of UV Maps you know you
might have two or three different UV maps and you might be using them in your materials to drive the appearance of
your object or you might just have a like a UV map like this just one UV map
you might not even be using this node but I'll keep it here because I have to because otherwise this will default to
to uh generated coordinates instead of UV map and if I want it to scale I might
do this you cannot use this UV map for baking a UV map for baking has to exist
within the UV area or if you're using udims it has to exist within the UV
tiles this is great for getting the appearance of your material so I can get
a nice tiling effect you can't bake to this because blender only bakes within
the UV space it just won't look right anymore a UV map for baking has to have
nice clean islands for every every point on your model every point of the surface
has to have a space somewhere in this Square so you can't use this so if I was
using this what I might do is I might say okay I've got that UV map I've
explicitly said here that's what's driv iing the appearance of this magic texture but what I'm going to do now is
I'm going to create another UV map and I'm going to call it simple bake and
it's not the active map so this is the active map obviously for rendering and we'll put it back on as active at the
end but I'm going to create this map and this map is going to be my nice clean map for baking and I'm just going to I
don't know just just to make sure we know what we're looking at I'm going to so it looks like that so remember that shape I'm going to put that back to the
the active UV map so that it's it's by default and then I'm going to go here
I'm going to bake this object I'm not going to generate new UV Maps I'm going to use the UV Maps I've got but I'm
going to turn on this prefer existing UV maps called simple bake option and just so we can see the results I'm going to
copy my object and I'm going to apply my bakes so let's go with
that so you see despite it having having a different UV map that wouldn't work
for baking active the UV map that we've used for baking which is the one so this
object is the object that simple bake's created and simple bake uh creates that
object and the only UV map it uses is the one that it used for baking so that your textures display correctly so it
doesn't care about the other UV Maps because once you're trying to just display your textures the only UV map
you give a damn about is is the one you actually used when you bake the texture so that's why this is the only VI map
that exists and you can see that it's that simple bake one it's not that default Cube one that goes outside the
boundary if we didn't use that option and we just let simple bake bake to this
UV map so it'll bake to this one because this is the active one highlighted so I'm going to turn that
off and I'm going to bake again
and you see that just looks completely wrong because you can't bake where your UVS extend beyond the UV boundary it
just isn't going to work in fact what it's done here it's it's thought it's it
thinks we're using udims because it can see we left this option turned on autod
detect udims so that UV map we were using it thought we were using udims and
that's why it's kind of done this and and why it looks terrible that's obviously not what we intended if I
reset and I tell it not to detect udims and I still don't turn this option on so
it's still going to try and bake to that UV map that extends outside the boundary let's see what we
get so again this is what we get you see it's missing a corner it's not at the
same like scale it's just it doesn't it doesn't bait correctly CU you have to
have a UV map that's inside the area so that that is a general kind of point to
take away you can have UV Maps any you know use whatever UV Maps you want scale
them take them outside the UV boundary uh whatever it takes to get your object looking like it should look but when
you're baking your object absolutely has to have a UV map that is within this
area not overlapping either so if these if I separate these islands
I can't have them overlapping because that's going to mess up they can't overlap everything has to be clean not
overlapping within the boundary and that's what that's kind of why this option exists so you have this
Other Settings (including "Copy objects and apply Bakes")
ability to create a separate map just for baking okay so now we're all the way
down to other settings so uh let's do this as quickly
as possible so batch name is as simple as it sounds it's just just a name that appears in your texture name so here
it's automatically set to like batch one bake one rather and that appears in the texture name if you remember from when
we were looking at the add-on preferences earlier in the video you have this format
string where are you oh there you are where you can control what's there so if you don't want the batch name you could
take it out of your string your format string if you want it in a different place and the text your name you can move it around but it's always just set
on the panel here to whatever you want in this text image sequence is for when you are
baking uh animated textures so all this will do is bake then it will advance the
frame by one which button is
that you can tell I don't do any animation can't you I've no okay here there we go so it will advance the frame
by one bake Advance the frame by one bake Advance the frame by one bake so it'll just keep uh moving the frame and
then baking so that can be quite uh quite handy if you're if you're doing that
kind of thing so let's see if we can show what it means so I'll do frames so
it's off until you turn it on and then let's do frames one to three bake image
sequence the only one we had selected was diffuse so what we should get is three theuse textures so three images
baked for three frames and you can see what we got here was the fuse. 00 1 002
00003 and those are the frame numbers so that's what I think that first textures from the previous bake we did in the
last example so what we got there was those three files for each of the frames
and that's all there is to that one really it's just looping the the baking process copy objects and apply bake so
this is another really important one so if I let's just make this
more interesting in cuz why not show there a chance to
compute um so let's bake this we'll turn off image sequence and we'll copy
objects and apply bakes so I haven't really defined any of the other channels but let's bake metalness roughness
normal map I don't know Alpha it doesn't matter because I've not actually changed any of these in the material but just to
show you so copy objects and apply bakes and hide Source objects after bake so
now when I bake it goes through the different Maps
bakes them one by one till it's complete and then you'll see it's hidden the object that we originally baked from and
replaced it with this Cube underscore baked in this simple bagore bakes group
and then if you go into that you can see that it's created the material and it's plugged in each of the textures that we
baked into that material so this object is actually what gets sent external if
you remember from when we we looked at the export mesh option it basically creates this object and then exports it
so this is the object you can that's why when it comes back in it knows the textures it was exported with because blender is clever enough to realize that
when you're exporting something with a material like this it writes this it writes a version of this material into
the fbx file so the other applications and blender itself recognize it when it comes back in but if you want to if
you're only interested in what happens in blender you'll see that basically we've if I unhide the original object
we've got this object so this object is using the original material this object is using the textures that we've just
baked and that's what the copy objects and apply bakes function does and that works no matter what you're doing so if
if you're um so if you're baking
multiple cubes as you so often are multiple objects to one texture set
it's fine with that as well so it'll go through it will bake all of those I didn't turn on the UV option so it's
baking on top of each other but I suppose it doesn't matter for this uh it's doing exactly what I warned you
about in one of the previous parts of this video but uh you can see that it's it's the they both share a material
referring to this merged bake texture so yeah copy objects and apply bases it's
exactly what it sounds like it copies your objects and it applies your
BS create gltf settings so you'll only need to know about this if you're exporting to a glcf file which I have
never actually used myself but I think it's quite popular in uh web like 3D
rendering applications and things like that so the way blender does this is that you have a
gltf settings node file um so if I just
bake one object to show you really quickly why is this you have selected AO
but you're not baking AO so again simple Beck is always trying to tell you what you've not quite done right so I'm going
to put AO on there so I get an AO map as well and then the object we get is here
and you'll see that it's created this gltf group this empty group that's how
blender does it and it's plugged it into the AO socket so what happens there
is when you export it to gltf blender recognizes this node recognizes that AO
is the setting you want for the occlusion and adds it in again you won't really need to know about that unless
you get into gltf um exporting and then you'll
probably know all about it yourself anyway but that's that's why that option exists uh apply BS to original objects
it will it can't coexist with copy objects and apply bakes which is why that option disappears when you turn it
on but this again does what it sounds like it will bait the object but instead of creating a new object and applying
that new material it will apply that material to your original object in
honesty this is quite dangerous not well not dangerous but it it it's potentially confusing
because it will replace the material on the original object so if I show you that
now I'm bacon cube. 001
it'll go through the different Maps I've asked for and then cube. 001 um has the has the same material
that I would have got for my copy and apply object but applied back to the original object itself the original
material is still in blender uh simple bait marks it with a a forced user so
that it doesn't get accidentally erased but it it it can be quite confusing and this is why it comes with this little
warning when you turn on this option that basically says be careful when applying brakes to original objects original materials won't be deleted but
they will have no uses and will be purged on next save I don't think that's actually quite true I think I changed it
so it forces it but I I I want people to be very careful with that option anyway so I might change that message or not um
and then this about restoring original UVS after bake but you're applying bake textures again we're we're applying our
bake textures but we're and we're we're creating a new UV map or we might be
creating a new UV map but um if you're
restoring your original UVS then it won't display correctly because the texture that you bake has to be applied
with the UV map that you use to bake it so it's just a little warning there to make sure you actually kind of think
about it and know what you're doing but again that option is is kind of used with care we've talked about Don't Force
High bit rate normal Maps simple bait will normally up all the settings on normal maps to Max so internally
there'll be 32 bit going external PNG there'll be 16 bit going external exr
every well everything's 32 bit but you know uh if you don't want it to behave like that and you want to treat normal
Maps like any other texture turn on that option and it will just treat them like any other
texture this I mentioned ages and ages ago in the video um surprised of you
know full gold star if you remember but back when we were talking about the user
preferences there's an option there for sample count for PBR baking uh and I said that simple bake automatically
boosts the sample count when it picks up a node in your material that needs it so
this is what that is it's saying that if it detects an AO or Beval bevel node uh
it boosts the sample count to 50 because despite the general rule that you don't care about sample count for PBR baking
you do care about it if there's a bevel node or an AO node mode so that's why it
boosts the sample count for those and if you want it to be higher or lower you can change it there finally GPU versus
CPU comput um I I mean that is what it is you undoubtedly know what that means
uh it just allows you to slep between the two of them and that's
that okay and now we come to channel packing which is one of the hardest things to demonstrate so let's let's go
simple let's start a new file um I'm just going to create a nice
Channel Packing
simple material we'll use my favorite magic texture again to give it a bit of
color maybe we'll plug in a noise texture into the roughness
Channel just so we can see it for illustration purposes so rough that roughness texture looks like that uh
that magic texture looks like that and we're plugging them both into the same material so we're going to bake this object uh
uh 1K is fine we don't want any special Maps we do need to do we need to export
it can't remember my own rules um I think we do need to export it
so I'll export it into my directory into the default default place again let's
just clear that out so we know what we're looking at and then we'll go all the way down to
channel packing so Channel packing is every material has basically every
texture every file any every image file has like four channels red green blue
Alpha now traditionally they just store exactly what they sound like the red Channel stores the red information in
your image blue the blue green the green and Alpha controls where it's
transparent and where it's not transparent and that's it however with
and this is not an area I'm an expert in at all but in things like game engines where you're trying to minimize memory
usage it's handy not to have to have lots and lots of separate image files or
Texture files it's handy to be able to kind of smuggle data in through the
channels so a really common one is this one where let's say we you can give it a
name but we'll just keep the default for now you can choose between PNG and TGA but a really common one is to have your
red information from diffuse your green information from theuse you blew information from diffuse so basically
you're just getting diffuse you're just getting what you see but then in the alpha you um you can only select the
sorry you can only select the maps that you're baking in the alpha you hide the
metalness hide's probably a bit strong but so diffuse diffuse fuse and then in
Alpha you hide the metalness so I'm going to add that now and then that's one of the texture the the channel
packed textures simple bake is going to create so you can have as many as you want in this list and you edit them just
by clicking on them and then when you're done you save it make sure you save it a big red that you saw that big red button
alerting you to the fact that you haven't actually committed your changes yet so I'll show you what that means if
you remember what our material looks like magic plugged in here here we've got a we do have a roughness texture in
fact oh almost made a mistake there let's also plug that into metalness cuz we're not actually we're not actually
using roughness are we we're using diffuse diffuse diffuse and metalness so I'm going to bake that and it will bake
diffuse it will bake roughness and it will bake metalness and then at the end it will create a fourth texture being
the channel pack texture oh we didn't we didn't select roughness my mistake so it bakes diffus metalness and then this
channel pack texture now this just looks normal but if you use this button to
isolate the channels you can see that the red is the red information from
diffuse the green is the green information from diffuse and these are that magic texture this is kind of why
it exists I think it's it's a bit of a diagnostic tool so you can see those are that's the blue information and these
are all black and white because they're effectively a a binary map like where there's white there's blue and where
there's black there's no blue that's that's what that's saying so same for green same for red but then in the alpha
you'll see we got that noise texture if you remember the noise texture we plugged in to the uh
metalness became our Alpha channel of our pack texture and that's how that
worked and the same thing gets sent to the external file system so you can see here it exists here as cube
uh underscore PBR pack texture if I open that in which is probably the
easiest way to see what we're talking about again it looks a bit garbled because it's not a a usual way for
to see an image but then if I go to channels I can isolate the red and again
it's still binary but just displays red or black instead of white or black I guess to make it a little
more obvious there's the green information blue information and there's the alpha information which is actually
not really Alpha information it's this metalness what what we had for
our metalness texture hidden in the alpha channel of the
texture and hopefully that made sense because I cannot explain it any better
than that it again it is a thing for mainly I think game engines uh it's a
way to not have loads and loads of texture files because if you think about it if I wasn't able to do that I would
need two textures I'd need my diffuse texture and I'd need a separate texture for my metalness map this way I get just
one file I wouldn't even use these in the game engine I don't think I would um hit the wrong button there I would just
use this and so I can just have one file and that's lower memory usage and it's
more efficient and it's better and everyone who uses a game engine loves it and everyone who doesn't doesn't really
understand it and I kind of fall into the latter although I I kind of understand it cuz some very kind
simple bait users have explained it to me uh so that's that's kind of how that
works uh as I said you can change between two different versions I talked again right at the beginning of the
video about uh there's an option in the preferences to enable the old version of Channel packing you won't see any
difference on this panel except for the fact that you can select exr I can't get exr to work with the new way of doing
Channel packing I'm really hoping that Third Way of Channel packing that I mentioned that I'm working on will just
fix all the problems um but for now it's PNG or TGA it works it works perfectly
fine um I I just wish that I could do it in a in a better way because this is
kind of hacky I'm kind of editing pixel by pixel and I just I don't really like it but it works it does work people use
it it's it's a useful feature um delete bakes after Channel pack so this would
just basically if you don't want the diffuse and metalness texture on their own and you're only interested in the
pack texture similarly on the export as well you remember I said oh I wasn't
going to even use the diffuse of metal I deleted them well they wouldn't even be saved out if I had that option turned on
it would only be the channel pack texture because that's all I'm interested in and I'm pretty sure that covers
it yeah I think that covers it I think that's it onwards to the next one
Utilities
so next section of the panel is utilities I've only very recently added this so this is brand new to simple bake
more or less so there's just a bunch of kind of things I thought would be useful so sometimes people change a lot of the
settings and then they can't work out what they've changed that's kind of giving them the results they don't want
and they just want to go back to the start and there wasn't an easy way to do that because simple bake saves all its settings into your blend file so as soon
as you open the blend file your settings are back and even if you uninstall simple bake and then download it again
from blender market and then reinstall it as soon as you enable it all your settings are back because they're stored
in the blend file so unless you're going to create a new blend file you can't get rid of them this button just purges all
the settings and you're back to completely to to start it purges all
your blend file presets because they're stored within the blend file but your Global presets are okay because they're
stored externally in the file system so that's just something to keep in mind restore all materials so if simple bait
crashes because you know if there's a bug or something and it it does it does happen from time to time then sometimes
what simple bake's doing with your materials because it obviously needs to mess around with your materials quite a
lot to achieve baking especially for PBR it's it's moving nodes and deleting
things and doing all kinds of stuff but it does create a backup of your materials that are in the blend file so
this button just automat atically deletes all the material simple B created and restores your original ones
um not sure there's an easy way to kind of illustrate that unless I forced it to crash which is more trouble than it's
worth but basically yeah it delete every material that simple B's created and restore everything from backups that it
creates itself uh this just create this just gets rid of every image simple baker has created this being one of them
so if I click this it's gone it just deletes every image that simple bake created obviously simple bake didn't
create it leaves it intact and then this deletes every object that simple bake's created the only objects simple bake
really creates are that cage object if you remember the the automatic cage
object so that's an object that simple bait creates and also the copy and apply
object if you remember that copy objects and apply bakes it duplicates your object and apply to bakes but this will
just get rid of every object that simple Vex created and this is just a little utility with a find and replace so you
can search through images objects materials and replace it it does support basic wild cards but they're a bit uh a
bit hard to understand they're not quite like like wild cards in Microsoft Word
or anything uh so if I do wild card be so I'm trying to Target that Cube object
and then I need to set so it'll match CBE basically because it's anything
followed by be and then I might want to change it to sphere um uh find and replace oh no not change
it to objects find and replace and you see it's renamed Cube to sphere you can limit it to simple bait created objects
images or materials or you can have it operate on everything in your scene it's just a little handy hopefully handy
Background Baking
utility and that's it for that one okay so let's talk about background
baking so there's two ways in which you can bake in simple bake you can bake in a foreground which is mostly what we've
seen and to be honest mostly what I'd recommend but you can also bake in the background and what baking in the
background does is it it fires up another instance of blender that you
can't see and that does the baking and then you import the results back into
back into your regular version of blender also if you've got options turned on to export to the external file
system so if you're exporting your bakes or you're exporting your um mesh it will
still do that in the background so I'll show a quick example of that really quickly so let's bake I don't know these
three I've not edited them so you know they'll be blank textures basically but it'll show you what I mean uh yeah let's
export it why not let's export them both I'm just going to go to where it's going to create those and get rid of it so you
see we've now got a blank simple bake uncore bakes directory uh we'll leave it everything
else as is and I'm just going to bake in the background so I can give each background operation a name so we'll just give it a
name and away it goes and you can see it's cued for a couple of seconds and then it will become active and then it will bake uh I can oh it's already
finished so this doesn't update instantly and this actually completed so quickly that we skipped active and went
straight to complete so you'll see that because I had the export options on it's created the images and the fbx and the
file system like it should have done nothing in blender yet because it all
happened in the background but if I do want that stuff in the foreground I can click here to import it it's imported
three images and zero objects zero objects because we didn't turn on this copy objects and apply bakes option so
it only created textures but now those textures exist here as well if I wanted to use
them um I think that's about it really you can also if you you can cancel
something Midway and it'll attempt to kill the background process there you go that's a little more obvious what it's
doing uh but also if you decide when it's finished that you actually you don't want the results you can just
click here and it just cleans up in the background basically if you've exported your files it won't delete them or
anything see that they're still here uh it just cleans up and gets it gets rid of the thing from your list the
background task from your list and it deletes the temporary files and stuff in the background that it was using to do
that and that's that's background baking really oh it's probably worth mentioning not everything is available in the
background just because of limitations on what I can do so again going back to no groups being such a pain in the ass I
can't ungroup no groups in the background because well I'm not going to explain why but it's just it's just
really annoying uh so if you try to bake something it says no groups detected unfortunately these can only be baked in
the foreground so I'd have to manually remove the no groups so I could do it there's a couple of other things that
don't work in the back ground like um image sequence image sequence can't be baked in the background and again it's
just annoying kind of limitations in the blender API that I can't at the moment
find a way around um I mean background is is useful uh but I do most of my
baking in the foreground to be honest it's useful to be able to fire off several background bakes and you can
fire them off and they will queue up like this uh and then they will just gradually progress from active to
completed one by one so if that in that way you could kind of queue up a bunch of bakes if you wanted them but uh yeah
if it's useful to you use it just beware like I say there's limitations on what it can
CyclesBake
do okay I think we're finally into Cycles bake so
this is the more let's reset all the settings shall we using that button uh
this is the as I've said before it shares a lot of the same options with with PBR bake and I'm not going to go
over them again but it's the traditional way blender bakes so if you set your
engine to Cycles because blender only allows baking in cycles and you were baking here it's basically this but with
the convenience of simple bake creating your textures creating your bake texture nodes setting all your settings yeah
it's this but with all the hassle taken out of it so you'll notice that if you've ever done kind of normal B in
blender you'll notice the options are basically the same so if I add a bake object like I'm going to bake this Cube
this is the main difference you can select these modes of baking which are the same as the ones you get in there
you can turn off so combined is I'm not going to go into every mode in which
blender can bake but Cycles bake in general is taking into account lighting and shadows and you can turn off
different elements of that to get the exact texture you want and then you've got these other bake modes like AO and
and glossy and diffuse and if you know what you're doing and these are all documented in blender's documentation as
to what these are but this is the kind of the old fashion the traditional way the non-pbr way that blender offers
texture baking uh sample count is pretty obvious because unlike PBR sample car is
extremely important for Cycles bake because you're dealing with light and Shadow and that gives noise um color
space is just this option again that you'll recognize from the add-on preferences when we were setting it for
the different types of PBR bake but this is just the the color space which your image will be created with so if I
quickly bake this I get this and then you'll see it's
created with whatever color space I selected so non-color oh no we're
looking at the wrong thing we're looking at the PBR texture Cycles bake so it's been created of srgb because that's what
we had selected so hopefully that makes sense uh we talked about denoising so I
won't go into it again but it's exactly the same as when we talked about it in PBR uh you can't use the compositor
because I'm not exporting my bakes but again we talked about this in the PBR section so flick back to that we talked
about it for PBR when we were talking about light Maps because on the PBR side of things this denoise stuff appears
here because it's not relevant to the main PBR textures it's relevant to light map Cycles bake is Rel to everything so
it appears here in this main part of the panel but it will also apply to these as
well hopefully that makes sense um I don't think there's anything else to
cover there I think that's it that's the that's the main kind of section of the Cycles bake panel I'll just pause the
video while I think about if anything else is different so I think everything else is
CyclesBake material type
pretty much the same um you've got the same um set of options for UVS the same
so have options for textures multiple option multiple objects to one texture set is the same um it's very similar
because it's really just the type of bake you're uh you're doing I guess the only thing that's worth pointing out is
that when you copy objects and apply bakes you've got this option about what your material is based around so there's
no kind of right and wrong way to create materials for Cycles bake um they can
either be fed into an emission node fed into the principal bsdf node
uh or background so fed into a a background node um if you know what
you're doing you'll know which one you want but I think most of the time you'll
want to feed it into an emission node and the reason for that is that that will then so just looking at this
material see how it's created going into a background node the reason is that when you're baking recycles bake I'm
going to sound like a broken record here but you're baking all the light and all the Shadows into the texture it's like you've taking a picture of the surface
so you don't really want to display that on your model in a way that adds more Shadows and Light because you've already
baked in the Shadows and Light that you want so what you tend to do is you tend to display it with an emission node set
to one set to a strength of one and that basically says it's not going to accept
any more light or Shadow from any other objects because emission surfaces don't
accept aren't affected by light or Shadow uh and it just displays it as
like a photo of the original material with the original lights and shadows so that's why I think most of the time
you'll just want it fed through an emission emission node at strength one
uh people who are more clever than me may want different kind of um different
options if you're wondering what's going on here it's telling me that the object I want to bake is hidden in the viewport
because I told it to copy and apply baking hide I could have simple bake just turn that back on but I'm I'm
constantly trying to not make simple bake do things without telling you because I hate when add-ons just do or
computer programs in general just do things and I I want to know what it's doing and why so everything simple B
does it's constantly trying to tell you so that's why it behaves like that yeah um I think that most of the time you'll
want to run textures through an emission node and that's sort of why that's the
default other than that little difference and the the main bit the
Cycles bake panel that we talked about everything else works the same in
honesty I kind of thought I was going to have to spend more time on this section but I've just flicked through and it's all pretty much the same um so I think
Bake to target (Standard)
we can probably leave it there okay I've saved the most
complicated bit for last I've also had about three goes at recording this so
far I don't know why this bit so hard to explain but okay let's try again let's
take this nice and slow so I'm going to talk about baking to Target so there's
different terminology here to think about uh people often talk about baking
high to low cuz normally well typically you have a high poly object and you're
trying to bake it to a low poly object uh not always but a lot of the time
that's why you're doing this the other terminology is source to um Target or
even blender calls it baking selected to active but it's all the same thing
you've basically got an object with materials and you want to capture the details of those materials and transfer
them to another object your target object your low poly objects or your active objects as blender calls it
simple bake I'm we call it a Target object or I call it a Target
object so we'll start by looking at the simplest case first so I'm going to
create myself a source object the object with the details we want to capture
let's use our favorite magic texture again um and we'll maybe make this
slightly more complicated than normal so I'm going to subdivide it couple of
times maybe I'm going to going to add a displacement modifier with
basic yeah something like that I'm going to subdivide it again just so it's
smooth yeah that's good okay so we'll have that as our high poly object and
I'm I've given it this magic texture as our shading and that's our object that
we want to capture so now I'm going to add a source object which should be a uh
sorry a Target object which should be a kind of rougher version often a lower
poly version of that object so let's simulate that I'm going to duplicate
that object and then let's I don't know decimate it no I tell you what we're not
going to get a good result by doing that because I won't be able to give it a good UV map let's do something different
let's just just add a cube and let's subdivide it a few
times and let's leave it at that so we still have a we still have the ability
to give that a half decent UV map uh that'll do I mean it'll have some
stretching because that's not the shape anymore but that'll do and we'll have that as a kind of low poly object maybe
make it a bit bigger so there you go so we're trying to make the detail of our high poly object onto our low poly
object or our source object to our Target object so I'm going to add our
source object to here and you could have more than one so in this simple in the simplest mode you can have multiple
objects in here and you might be baking them all down to one target let's give them names so I don't confuse them so
we'll call that source which is again Source H poly whatever and we'll call
that Target and we'll set that as our Target now I'm not going to change any
other options let's just see what kind of result we get from that so it's not too bad uh you can see
areas here I think that's Pro and there as well uh where I think this is
breached you see that the low poly object doesn't completely Encompass the
high poly object so the way I think about this and I think this is correct but I'm not an expert in this area at
all is that what blender is doing is that it's firing Rays out from the target object and trying to
intersect the um the source object so firing firing Rays out from the low poly
object to try intersect the high poly object and what you can do is you can
play with these set settings so we'll talk about cage objects in a second but
if you don't specify a cage object blender's going to kind of almost most sort of say well fine I'm going to create my own and it's going to create
this automatic cage if you like defined by your cage Extrusion value and your
ray distance value I'm not going to try and explain those because again I'm not an expert on them but you see that these
settings are pulled in from blender so these popups well this one isn't sorry
this one isn't this one is the pop-ups sort of give you a flavor of what they are so the cage Extrusion is how far
they automatically gen generated cage is extruded and the ray distance is how far blender is going to fire those Rays
before it gives up and says there's nothing there um it's really fiddly in honesty I've used other applications to
bake from one object to another and I just find blender's approach to it fiddly really fiddly or maybe I just
don't know what I'm doing you know probably true um but you you can fiddle
with those values this is a simple bake specific value that is just a multiplier of both of them those and that was
suggested by a kind simple bake viewer viewer it's not a TV show that was
suggested by a a kind simple bake user um as a good way of kind of easily
dialing in those two values rather than having to change them both independently you can kind of just change them with a
single multiplier and it is Handy to be fair this was also suggested by a simple
Pig viewer user uh and this is kind of it was kind were kind enough to explain
to me that the different options you have enabled in blender's uh default settings for baking selected to active
control how it processes the automatic cage whether it's smooth or whether it's
hard it's quite complicated but it does make a noticeable difference so I
implemented it in simple bake and this is just a convenient way to switch between those two
options having said all of that it is by far better to use a cage object a cage
object is basically a inflated version of your target object or your low poly
object and the way I think about it the way it's been explained to me is that instead of firing Rays out from the
target object to or the low poly object and trying to get them to intersect the
high poly object it fires Rays inwards from the cage so the Rays get fired from
the cage pass through the object The Source object the high poly object the object that you're trying to capture the
details from and then they travel inwards to the Target object is that
correct I don't know but that's how it was explained to me and that's how it seems to work so that's how I think
about it in the latest versions of simple bake we've made this easier or I've made this easier by generating by
Bake to target (Automatic Cage Object)
adding this option to automatically generate a cage object so when you click
this you'll see that it's taken my target object and it's created a a kind
of low poly version of it or or a simplified version of it and then I can expand or contract that so what you want
to do is tune this so that your cage is
encompassing all of your high poly object so it's quite fiddly isn't it
don't know if I can change that but if I adjust the volum manually
no still popping out so let's add another two to that no still five to
that okay so that's now encompassing everything without going too far I mean
this isn't a great example because your target object should match your uh Source object or your low poly
object should match your high poly object a little better than I've got here but let's see let's see what result
we get so we don't need to worry as much about the so this is just left us unlimited this isn't even relevant um
because we we're using our own cage so let's see what we got okay so it's more or less oh it's
actually yeah it's actually a lot better so here where we had those artifacts those hard lines we don't have
them anymore I mean a better way of showing this might actually be thinking about it if we make a normal map does
that show so going to just remember 0.15 is what we had for that cage so I'm just going to bake it
again let's just check this normal map yeah you can see where it's
breaching the surface and you get these weird anomalies now let's
see you think I would have tested this wouldn't you but we're going to find out together does this look better it does
it does look better okay that's good I don't have to re-record this again so that see that the cage object has given
a much better result and we've captured the normals of that object uh the reason it's faceted like this is because it
takes into account the normals of your low poly object as well you probably want to deal with that smooth them out
but um you can see that it's captured the detail of the high poly object onto the low poly objects much better with
that automatic cage so yeah that works really well actually um so that's all
Bake to target (Auto-match high to low)
the simplest way of of um baking selected to active or source to Target
or high to low or whatever it is you want to call it um the only other thing
I'll mention should I mention no let let's go on to the other methods I just need to
remember to come back to this isolate objects option um so I'm going to move on to auto match high to low which is a
different method so in the not in the method we've just been discussing standard is baking everything in this
list to a single low poly or Target object in this mode this is for when you
might have lots of pairs in your scene of usually High poly and low poly
objects and you want to bake them allall and you don't want to do it one at a time so this automatically matches High
poly objects with low poly objects so you add your high poly objects to the list you see there's no option to
manually specify a low poly object cuz it's trying to automatically detect them
at the moment it's trying to automatically detect them by position so
you'll see at the moment it it's found this the object called source and it thinks it's going to bake it to Target
because it's sh you know they've got the same origin basically if I move Target
away it if I move Target
away why are you doing that oh it's cuz the the CAG is left behind right hold
on let's get rid of all simple bait created objects so if I move Target away
it can no longer find the object it wants to bake to because it's basing it on position and it's too far away it has
to be back there again and now it's found here again the other way of matching them is because is by
name so here if I switch to name it's cleared the list because the only as
explained in the tool tip only object objects with underscore high as a suffix to their name can be added to the list
each object's icon indicates corresponding match so for this this depends on we'd have to rename our
objects so we couldn't say Source we'd have to say something like I don't know Cube
high and then I can add it to the list and then it's looking for a Target by
name this has to be Cube low and you the high and low can aren't case sensitive
but they do have to be high and low and now regardless of its position it's going to try and bake to that obviously
this is going to be nonsense because they still have to share the same space but it's not relying on the position
anymore it's it's using the name and then all that will do is it will go
through and bake them uh one by one so we've only got one match in our
list but it's going to go through and bake them one by one if you've got more than one match
it's detected udims which must mean that our uh our UV
map is strayed into that tile slightly but um yeah well we won't worry about
that for now but that's how that works so you get these auto match things and it'll bake so it's bake diffus and
normal because they the ones we selected if I had two pairs I'll just show that really quickly
so we'll duplicate both these objects we'll call this one Cube uh we'll call this one Cube two high and we'll call
this one Cube two low and we'll add Cube two high to our
list um let's see if we can quickly fix that UV map problem it's just straying
over the line so it thinks it's udims so we could either scale the UVS or I could
turn off this Auto detect udm's option but I'll just scale the UVS because it's quicker so now I'll click it uh and it's
going to do both of those objects it's going to find them both it's going to do
one uh diffuse normal then diffuse normal and then you see we get left with
both two textures for the two different objects the final mode is decals um I'm
going to pause the video for a second because I will um open a file I've
prepared specifically to demonstrate that so so I'm recording this uh after I
recorded the original bit of the video about baking to Target because I said I needed to come back to mention the
Isolate objects
isolate objects option and I absolutely didn't so really simple uh
the isolate objects option is most relevant for Cycles bake when again you're talking about baking uh objects
get baked taking into account light and Shadow and things like that less relevant for PBR because it's all just
baked as a mission there's no light there's no Shadow baked into the texture so they can't really interfere with each
other I still have the object uh the option available here because I think it can make a difference in some
circumstances and I think it especially if you're baking like a light map in PBR
mode it will it will affect as as we discussed when we were talking about special Maps special BS light maps are
basically a Cycles bake because that's how they work so it's more relevant there but it's most relevant here in
Cycles bake um so if I was baking this Cube for
example I might turn uh sorry if I was making this Cube and just let's have it
normal combined and let's just quickly turn it to GPU and let's just
quickly bake that and see what we
get so you can see that there's a big chunk missing from
here because that's where this this uh object this other cube is obscuring this
Cube so the light source coming from prob well that's the only lamp in the scene so I guess all the lights coming
from there uh is being uh obscured so now if I turn on the isolate objects
from each other it should hide this object from rendering so it doesn't affect any rendering including texture
baking and we should see that the other object is baked without that object
obscuring it there we go sometimes you have to zoom in and zoom out a bit for the image
to refresh by the way if you're wondering why it still looked wrong for a second there but yeah that's the
texture we get so it's just isolated that object so the classic example of this that I like to use is you're baking
a character model and they you've modeled like a belt or gloves but you
want to bake the textures of their body or their like underlying cloes without
the effect of the belt or the gloves or whatever it is so instead of having to
manually hide them delete them or take them out of the scene you just tell it you can tell simple bake to isolate the
objects from each other and therefore the gloves and the belt won't interfere with the the texture baking for the body
if you're using the bake to Target mode uh this functions slightly differently
it's explained in the tool tip when baking to Target this hides the influence of all objects except those in
your bake objects list so if you're baking some uh some objects to Target so
you've got a list here but you have other objects in your scene that you don't have in this list and you don't
want them to affect the baking to Target that you're doing then this functions to
hide them so so it will only it won't hide anything in this list and only
these will influence your target object it works slightly different again in the
auto match mode because then what this will do is that it will basically hide
everything in the scene except for your pair of high to low which is what you
it's basically what you'd expect if I wasn't explaining it to you it's it's hiding the influence of objects other
than what you'd naturally expect so I think in this mode where you're pairing objects you'd expect only those two
objects to be relevant so you'd expect everything else to be hidden uh which it
is that's how it works and when you're doing this kind of Standard Baking to Target it's going to hide everything
that you've not specifically said I want this in to influence the bake and putting it in this list and that's
basically how that option works okay so this is a file I've
Bake to target (Decals)
prepared specifically to illustr rate the process of baking decals this exists
as its own separate mode in simple bait just because it's quite a common thing that people want to do so the first
thing I'm going to do is I'm going to reset all the simple bake settings so I can show you how to set it up and I'm going to show you what we've got here so
what we have here is a cube uh this is an object it's weird so these are these
are decal um let's start with these These are planes with transparency so
you can see that they are what you would typically understand to be a decal they're stuck to the surface of this
cube with uh modifiers shrink wrap modifiers you don't really need to do
that but I think it's the easiest way of making sure that they stick to the cube and the cube is just a cube uh with that
magic texture that I love so much applied to it for testing purposes now
these are obviously three separate objects each with their own texture what I really want is to create a texture or
to create a material rather that is just a single object with a single material and properly displays those decals so
again it's a really common use case uh in simple bake and this is how you would do it so you start by adding the bake
objects that you want are the decals so we have decal one and decal 2
and then we turn on bake to Target and set this to decals mode decals to Target
so decals to Target uh and then the target I'm going to choose as the cube you won't bother with
a cage object because as I say I've used modifi to stick these things to the surface of this Cube so it's very
unlikely that uh we'll get any problems with Ray distance and things like that so we'll just leave it there we won't
use a c cage object and I'll just leave these Extrusion settings at default and I'm hoping they will be okay so the
other thing we need to consider is what we're baking we don't have a choice in Alpha because de FS almost always have
Alpha so you do always want to bake Alpha so I can't even turn that off but the rest of it depends on what
channels both your decals and your target object are using because they're
all going to be in the end result so the new function here to autod detect should
be smart enough to pick that up so it's picked up metalness emission emission strength and diffuse as the the channels
that this object should need so it's going to bake all of those first for this second for that or maybe vice versa
and third for this so the only other option we'll turn we'll think about
we'll leave UV Maps as is because I've already checked and the UV map for my
target object looks good um well not good it looks okay and
the only option I'll turn on is copy objects and apply Bas because I want to show you the material that it creates
for the those decals and so we'll just go we'll bake so now it's going to cycle
through um it will take a little bit because as I think I've mentioned previously in the video blender only
lets you send one object to the bake engine at a time um I've got a couple of
ideas on how to overcome that limitation in the future but for PBR baking it
doesn't take that long anyway cuz like you say you can see they're just they're just pretty quick um and this is the end
result you'll see that it's hidden the original Cube and the two decals and
left us with this single object with the decals baked in so let's look at the
material looks pretty complicated but this is how you should really display
decals so each of the channels we've baked let's look at diffuse because it's simple consists of the diffuse from the
the underlying Target object so our Cube and then the decals the fuse from the decals and then
an alpha that tells it an alpha mask that tells it where to display the
decals so it will display the decals where it's white and not where it's black and they are fed into the
principal bsdf same for metalness same for emission same for emission strength
and then Alpha is driving it all as um yeah your Alpha texture is just white
really because you want to display the whole of the mixed texture and then the
normal map isn't actually being used cuz did we bake it I can't even remember no we didn't even bake it so
it's not actually uh doing anything so that's what you get you can actually go even further if you if you want to so
depending on what this might be all you need this might be what you were looking for this might be what you're using but
you could go even further if you really wanted to match the textures together so
there's no remnants of of what's left you could uh bake it again so I can add
Cube baked to my list let's just Auto detect again uh and let's
uh copy objects and apply BS again oh yeah sorry no we want to turn off bake
to Target because that's not appropriate for this
one and now what we'll get hopefully
eventually give it a second to compute those there we go so now what we've actually got is the textures properly
properly mashed together so um where are
we that's too many now
CU baited PBR diffuse there you go so it's actually created a single texture with
the decals mashed into the all into one
texture basically flatten them all into one texture so depending on what you need you might want to bake it and then
bake it again to reduce it all down I might look at a way of having it do that all from the start eventually but at the
moment um that's that's the workflow for that so hopefully that covers
Wrap-up
decals okay um I think that's it I had a click through the simple bank interface
when I paused the video looking for anything I'd completely forgotten to cover I couldn't see anything but simple
bake is ironically so complex at this point I wouldn't be surprised if I've missed something uh if you drop me a
note maybe on Discord I'll try and make sure that I cover that and upcoming video if I can uh as I say please come
over to Discord if you especially if you need workflow advice from other simple bake users if it's a bug or a problem
where simple bake isn't working as it should then contact me probably via blender Market because I do formal
support through the blender Market inbox um I do also get a lot of workflow type
inquiries and if I can or if I have time I do try and help people but in honesty
there's so many especially people who are new to blender or new to baking and looking for that kind of help I can't
always really give it um between developing simple bake and you know my actual life and job and stuff it's
difficult to find time always to reply to to people looking for that that help
so Discord is the best place for that because then at least there's a chance another user might be able to help you
with uh with workflow and and basic understanding queries but certainly if it's a bug you know if it's a crash or
if there's just something you that's blatantly not working as it should then drop me a message on blender market and
I am very quick to patch bugs if you can provide a blend file or steps to
reproduce it that's the ideal because it's very hard to fix something if I can't reproduce it on my machine as I'm
sure you you will appreciate but uh yeah other than that like I say please feel free to come over to Discord um but
other than that thank you for your time I hope this was useful I'll try and do these more regularly um if I can find
the time but I will try and do at least like a couple of updates a year or something to cover the big changes to
simple bake just so the tutorials I have up there don't go massively out of date
thanks very much