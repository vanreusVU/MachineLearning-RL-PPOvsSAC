# MachineLearningPIZZAHAWAI

## Algorithms that we will be using
PPO and SAC: https://github.com/Unity-Technologies/ml-agents/blob/release_12/docs/ML-Agents-Overview.md#deep-reinforcement-learning
<br>
Hyperparameteres: https://github.com/Unity-Technologies/ml-agents/blob/release_12/docs/Training-Configuration-File.md

## The concept of the game:
We basically have to agents. One of them chases the other one tries to run away.

## Sensors

### Raycasts
16 lines around the agent to detect obstacles and the other agents.
![image](https://user-images.githubusercontent.com/60816119/111170008-c5d27680-85a3-11eb-804f-93571b42df53.png)

### Collectable Observations
We are getting live info from the game and feeding it to the n network
![image](https://user-images.githubusercontent.com/60816119/111170648-75a7e400-85a4-11eb-950f-55201c4c3f35.png)

## Rewarding
### Chasing Bob : The one who chases
Chasing Bob gets rewarded if he can manage to catch the Running Bob. (Positive feedback)
Chasing Bob gets punnished if he can't manage to catch the Running Bob in a given time. (Negative feedback)
Chasing Bob gets punnished if he goes to the edge of the maps. (Negative feedback)
Chasing Bob gets rewarded based on the time that he catches the Running Bob. (Positive feedback)

We later realized that this rewarding system isn't complete at its current state since the world is big and to achive good results with randomness we would need much more generations. To reduce the needed gens we created special cases where we reward the agent more if agen uses the platforms to go to the target.
![image](https://user-images.githubusercontent.com/60816119/111239310-2cce4a80-85f9-11eb-9f6f-d32974a1c858.png)
<br>

[Red line represents the: one without the platform rewarding. Blue line represents the: one with the platform rewarding]
![image](https://user-images.githubusercontent.com/60816119/111347235-4914ca80-867f-11eb-8a0d-09e1c9c66366.png)


<br>

(In the picture below you can see all of the target locations)
![image](https://user-images.githubusercontent.com/60816119/111233196-ce02d400-85ec-11eb-869b-2b7aa199ad9e.png)


### Running Bob : The one who runs
Running Bob gets rewarded if he can manage to run from the Chasing Bob until the end of the round. (Positive feedback)
Running Bob gets punnished if he goes to the edge of the maps. (Negative feedback)

## Training
### Chasing Bob
The way we trained chasing bob is based on randomly placing stationary targets on predefined locations in the world.
By doing this chasing bob started to learn how to get to mo each target after 200th generations (out of 500). However, this also brought some problems with it.

What we first realized is the agent always goes to as spesific spawnlocation first before trying to find the real target because what the agent realised is if he keeps going to that one spesific location he will get rewarded at somepoint. That's why the agent always goes to that point at starts exploring from there if he didn't get rewarded.In order to avoid this problem we came up with a different spawning system for the targets and we call it a heatMap.

![image](https://user-images.githubusercontent.com/60816119/112476007-bc01fd80-8d71-11eb-8df2-9ac917a56292.png)

Every spawn location starts with the heat of 1. Which means that at the start of the training all of the spawn locations have the same probability to be selected.
We select a random float between 0.1 and [totalAmountOfHeat] (inclusive). Which is 1 * [amountOfTargetLocations] at the begining of the traning.

![image](https://user-images.githubusercontent.com/60816119/112476282-071c1080-8d72-11eb-9085-42adbeb3e823.png)

After geting the random float number we select the spawnLocation that the randomNumber fits in.
Lets assume the random number is 3.86 and we have 4 spawn locations {A, B, C, D}

is [A] if: 0.1 >= x <= 1.0 
is [B] if: 1.1 >= x <= 2.0
is [C] if: 2.1 >= x <= 3.0
is [D] if: 3.1 >= x <= 4.0

in our case the random number is 3.86 so we choose de spawnLocation D

![image](https://user-images.githubusercontent.com/60816119/112476735-84478580-8d72-11eb-849c-8264bb40d557.png)

Eveytime the agent manages to find the target the heat gets reduced by 0.005 (untill 0.5) which also lowers the probability of that spesific location. Which means the range gets lower.

![image](https://user-images.githubusercontent.com/60816119/112477933-c0c7b100-8d73-11eb-8a66-29cac5aeb55e.png)


And now in every 100 generation the program creates a heatmap for the most successfull targets and starts to spawn them less and gives more chance to other targets so that the learning will be the same for all of the locations. 


## Rewarding
![image](https://user-images.githubusercontent.com/60816119/111834916-8e8dfd80-88f4-11eb-92a9-ac6c0cb3056e.png)



Second Traning method:

## Extra
We train for 500.000 steps. In every steps the agent recives an [Continues] input array of size 2 with float values between [-1 and 1] which is used in AGENT MOVEMENT (explained below). The agent can take up to max 1000 steps.

## Agent Movement
From the MLAGENTS GITHUB

Continues Input:
> When an Agent's Policy has Continuous actions, the ActionBuffers.ContinuousActions passed to the Agent's OnActionReceived() function is an array with length equal to the Continuous Action Size property value. The individual values in the array have whatever meanings that you ascribe to them. If you assign an element in the array as the speed of an Agent, for example, the training process learns to control the speed of the Agent through this parameter.

