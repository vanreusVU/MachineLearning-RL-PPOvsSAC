# MachineLearningPIZZAHAWAI

## Algorithms that we will be using
PPO and SAC: https://github.com/Unity-Technologies/ml-agents/blob/release_12/docs/ML-Agents-Overview.md#deep-reinforcement-learning
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
(In the picture below you can see all of the target locations)
![image](https://user-images.githubusercontent.com/60816119/111233196-ce02d400-85ec-11eb-869b-2b7aa199ad9e.png)


### Running Bob : The one who runs
Running Bob gets rewarded if he can manage to run from the Chasing Bob until the end of the round. (Positive feedback)
Running Bob gets punnished if he goes to the edge of the maps. (Negative feedback)

## Training
### Chasing Bob
The way we trained chasing bob is based on randomly placing stationary targets on predefined locations in the world.
By doing this chasing bob managed to learn how to get to the each target after 6192 generations.

We realized with this random method the agent tries to maximese the outcome by staying in one of the spawn zones with out trying to find the other ones. In order to avoid this problem we came up with a different spawning system for the targets.

Now what we are doing is first we start by spawning randomly. In every 100 generation the program creates a heatmap for the most successfull targets and starts to spawn them less and gives more chance to other targets so that the learning will be the same for all of the locations. 


Second Traning method:
