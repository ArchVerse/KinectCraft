package net.minecraft.src;

import net.minecraft.src.WoollySocks.NewMessage;

public class MovementInputFromOptions extends MovementInput
{
    private GameSettings gameSettings;
    public boolean kinectForeward = false;
    public boolean kinectBack = false;
    public boolean kinectStrafeLeft = false;
    public boolean kinectStrafeRight = false;
    public boolean kinectJump = false;
    public boolean kinectSneak = false;
    public boolean connected = false;
    private float forePercent = 1;
    private float strafePercent = 1;
    
    public MovementInputFromOptions(GameSettings par1GameSettings)
    {
        this.gameSettings = par1GameSettings;
        this.setUpWS();
    }
    
    public void setUpWS()
    {
    	socks = new WoollySocks();
    	socks.messageCallbacks.add(new NewMessage(){ 
    		
    		public void sendMessage(String message) 
    		{
    			String[] instructions = message.split(";");
    			for(int i = 0; i < instructions.length; i ++)
    			{
    				if(instructions[i].compareTo("move_foreward") == 0)
    				{
    					kinectForeward = true;
    					kinectBack = false;
    				}
    				else if(instructions[i].compareTo("move_back") == 0)
    				{
    					kinectBack = true;
    					kinectForeward = false;
    				}
    				else if(instructions[i].compareTo("strafe_left") == 0)
    				{
    					kinectStrafeLeft = true;
    					kinectStrafeRight = false;
    				}
    				else if(instructions[i].compareTo("strafe_right") == 0)
    				{
    					kinectStrafeRight = true;
    					kinectStrafeLeft = false;
    				}
    				else if(instructions[i].split("\\[")[0].compareTo("foreward") == 0)
    				{
    					forePercent = Float.parseFloat(instructions[i].split("\\[")[1].split("]")[0]);
    				}
    				else if(instructions[i].split("\\[")[0].compareTo("strafe") == 0)
    				{
    					strafePercent = Float.parseFloat(instructions[i].split("\\[")[1].split("]")[0]);
    				}
    				else if(instructions[i].compareTo("jump") == 0)
    				{
    					kinectJump = true;
    				}
    				else if(instructions[i].compareTo("sneak") == 0)
    				{
    					kinectSneak = true;
    				}
    				else if(instructions[i].compareTo("idle") == 0)
    				{
    					kinectForeward = false;
    					kinectBack = false;
    					kinectStrafeLeft = false;
    					kinectStrafeRight = false;
    				}
    				else if(instructions[i].compareTo("!jump") == 0)
    				{
    					kinectJump = false;
    				}
    				else if(instructions[i].compareTo("!sneak") == 0)
    				{
    					kinectSneak = false;
    				}

    			}
    		}
    		
    		public void connected()
    		{
    			connected = true;
    		}
    		
    	});
    }

    public void updatePlayerMoveState()
    {
        this.moveStrafe = 0.0F;
        this.moveForward = 0.0F;

        if (this.gameSettings.keyBindForward.pressed)
        {
            ++this.moveForward;
        }

        if (this.gameSettings.keyBindBack.pressed)
        {
            --this.moveForward;
        }

        if (this.gameSettings.keyBindLeft.pressed)
        {
            ++this.moveStrafe;
        }

        if (this.gameSettings.keyBindRight.pressed)
        {
            --this.moveStrafe;
        }


        this.jump = this.gameSettings.keyBindJump.pressed;
        this.sneak = this.gameSettings.keyBindSneak.pressed;
        
        
        if(this.gameSettings.mc.kinectActive)
        {
        	if (this.kinectForeward)
            {
        		this.moveForward += forePercent;
            }

            if (this.kinectBack)
            {
                this.moveForward -= forePercent;
            }

            if (this.kinectStrafeLeft)
            {
                this.moveStrafe += strafePercent;
            }

            if (this.kinectStrafeRight)
            {
                this.moveStrafe -= strafePercent;
            }

            this.jump = this.kinectJump;
            this.sneak = this.kinectSneak;
        }
        
        
        if (this.sneak)
        {
            this.moveStrafe = (float)((double)this.moveStrafe * 0.3D);
            this.moveForward = (float)((double)this.moveForward * 0.3D);
        }
    }
}
