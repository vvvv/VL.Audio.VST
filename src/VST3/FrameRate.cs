namespace VST3;

//------------------------------------------------------------------------
/** Frame Rate 
A frame rate describes the number of image (frame) displayed per second.
Some examples:
	- 23.976 fps     is framesPerSecond: 24 and flags: kPullDownRate
	- 24 fps         is framesPerSecond: 24 and flags: 0
	- 25 fps         is framesPerSecond: 25 and flags: 0
	- 29.97 drop fps is framesPerSecond: 30 and flags: kDropRate|kPullDownRate
	- 29.97 fps      is framesPerSecond: 30 and flags: kPullDownRate
	- 30 fps         is framesPerSecond: 30 and flags: 0
	- 30 drop fps    is framesPerSecond: 30 and flags: kDropRate
	- 50 fps         is framesPerSecond: 50 and flags: 0
	- 59.94 fps	     is framesPerSecond: 60 and flags: kPullDownRate
	- 60 fps         is framesPerSecond: 60 and flags: 0
*/
struct FrameRate
{
    enum FrameRateFlags : uint
    {
        kPullDownRate = 1 << 0,
        kDropRate = 1 << 1
    };

    uint framesPerSecond;
	FrameRateFlags flags;
};
