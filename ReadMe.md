ParseManagerCS Version!
TODO:
- [ ] Allow the use of functions in arguments (Tough)
- [ ] Allow the use of statements in conditionals: `if num+5>=GETAGE()-1 then STOP(song)|SKIP(0)` (Tough)
- [ ] Add other cool built in things (Fun)
- [ ] Add object support (Tough)
- [x] Improve audio support (Simple)
- [x] Add simple threading (Alright)
- [ ] Fix Bugs! (Death)

Maybe:
- [ ] Add While/for loops (With labels this can easily be done. So it isn't really needed, I may add it in the future though!)

This version is 1.0!
Writing code
------------
```lua
VERSION 1.0
-- Tell the interperter that this code is for version 1.0
ENTRY START
-- By defualt the entrypoint is start, but you can define your own if you want
LOAD data/OtherFile.dat
-- You can also load other files like this... This LOAD is different to LOAD() it must be at the top of the file
ENABLE leaking
-- When the end of a block is reached it will go to the next one... Disabled by defualt!
DISABLE casesensitive
-- when this is done 'NamE' and 'name' mean the same thing for variable names enabled by defualt
DISABLE forseelabels
-- Enabled by defualt, when disabled you can only jump to labels within the current block!
-- GOTO always searches the current block first then looks into others when jumping!
ENABLE debugging
-- When enabled a lot of mess will show up on your console... This isn't really useful to you as much as it is for me... If you ever have a weird error enable debugging and post everything into an issue so I can look at what causes an error
-- This language is still a major WIP!

-- Create a block named START
[START]{
	"This is a pause statement"
    -- This is a comment, comments in version 1.0 must be on independent lines like this
    -- A pause statement prints the string and waits for enter to be pressed if you are using the console!
	-- Version 2.0 Will have a few changes that allow pause statements to work a bit differently
	::A_Label::
    	-- Labels allow you to jump to a section of code from anywhere!
        num=0
        -- This language has a few types
        -- Numbers
        -- Strings
        -- bools
        -- Tables (A mix between lists and arrays) These are a bit bugged at the moment!
        str="string"
        bool=true
        tab=[1,2,3,4]
}
-- It is also useful being able to create your own functions
[callMe:function(msg)]{
	print(msg)
    -- We will just print!
}
-- An important note about functions: Everything except a functions enviroment is global
-- A function can read, but not write to the global enviroment! (You can get around this using labels and blocks)
-- You can use the method setVar("VarName",data) to set the global data
```
First we will look at flow control!
There are a couple of functions that deal with flow control
- JUMP(string block) -- Jumps to a block
- GOTO(string label) -- Goto's a label
- SKIP(int n) -- moves up or down line(s) of code by a factor of 'n'
- EXIT() -- Exits the mainloop keeping the language running
- QUIT() -- Exits out of the code at once closing the application
- SAVE() -- Saves the state of the code (Can be scattered around wherever except functions and threads!) This will bug out if saved in a function due to the stack not being saved! Only variables and position of your code is saved!
- LOAD() -- allows you to restore your saved session (Does not work within threads! Functions untested!)

Thats it in regards to flow control... All flow control functions are uppercase! While every other is lower camelcase

We also have a bunch of other built in functions to make coding eaiser!
- env=getENV() -- gets the current enviroment and returns it
- void=setENV(ENV env) -- sets the current enviroment
- env=getDefualtENV() -- gets the main global enviroment
- env=createENV() -- creates a new enviroment
- string=getInput() -- prompts the user for a string... use with write(msg)
- void=setCC() -- Don't excatly know why this exists Will probably dissapare in Version 2.0
- void=whiteOut() -- removes the last line from the console
- void=setBG(Color c) -- sets the color of the BG text... SEE: Colors for how to get colors
- void=setFG(Color c) -- stes the color of the FG text
- void=resetColor() -- resets to the defualt console colors
- number=len(object o) -- gets the length of a table of a string
- tonumber(string strnum) -- turns a string to a number if it contains a number in it
- void=sleep(number milliseconds) -- sleeps for some time
- void=setVer(string name,object data) -- sets the global variable to data
- number=ADD(number a,number b) -- Low lvl command for testing that survived, not needed just write expressions when you need it
- number=SUB(number a,number b) -- Low lvl command for testing that survived, not needed just write expressions when you need it
- number=MUL(number a,number b) -- Low lvl command for testing that survived, not needed just write expressions when you need it
- number=DIV(number a,number b) -- Low lvl command for testing that survived, not needed just write expressions when you need it
- number=CALC(string expression) -- Low lvl command for testing that survived, not needed just write expressions when you need it
- void=pause() -- pauses until you press enter
- void=print(string msg) -- prints a message with the newline character appended to it
- void=write(string msg) -- same, but no newline is appended
- number=random(number min,number max) -- returns a random number between min and max
- number=rand() -- returns a random number from 0 and 1
- round(number num,number numdecimalplaces) -- rounds a number
- void=clear() -- clears the console
- void=backspace() -- deletes the last character
- void=beep() -- makes a beep sound from the console
- void=fancy(string msg) -- Prints a message in fancyprint... See Fancy
- void=setFancyForm(string form) -- sets the form for fancyprint... See Fancy
- void=setFancyType(number type) -- sets the type for fancyprint
- id=loadSong(string path) -- loads an audio file
- void=playSong(id) -- plays it
- void=stopSong(id) -- stops it
- void=pauseSong(id) -- pauses it
- void=resumeSong(id) -- resumes it
- void=setSongVolume(id,number vol) -- turn it up or down
- void=replaySong(id) -- replays the song (Untested... It may of may not work...)
- void=setPosition(x,y) -- sets the position of the console
- void=writeAt(string msg,x,y) -- writes at a certain position
- bool=isDown(key) -- returns true if a key is pressed See Keys

TODO: Finish the readme...

