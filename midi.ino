byte buffer[3];
int pwms[5]={255};
int c[5]={3,5,6,9,10};
int TIME;

void setup() {
  Serial.begin(9600);
  TIME=millis();
}
void loop() {
  if(Serial.available()>=3)
  {
    int cmd_channel=Serial.read();
    int first=Serial.read();
    int second=Serial.read();
    if(cmd_channel>>4==0xE){  //0xE
      if((byte(cmd_channel&0xF)<8)&&(byte(first&0x7F)==0x11)&&(byte(second&0x7F)==0x11)){
        int ad=analogRead(cmd_channel&0xF+14);
        Serial.write((byte)cmd_channel);
        Serial.write((byte)(ad & 0x7F));
        Serial.write((byte)((ad >> 7) & 0x7F));
      }
    }
    else if(cmd_channel>>4==0xD){ //0xD
      int Channel=cmd_channel&0xF;
      for(int i=0;i<5;i++){
        if(c[i]==Channel){
          if(byte((second>>5)&0x1)){  //返回pwm值
            Serial.write((byte)cmd_channel);
            Serial.write((byte)(pwms[i] & 0x7F));
            Serial.write((byte)(((pwms[i] >> 7) & 0x1)|second));
          }
          else{ //设定pwm值
            pwms[i]=first|((second&0x1)<<7);
            analogWrite(Channel,pwms[i]);
          }
        }
      }
    }
    else if(cmd_channel>>4==0xF){ //0xF
      if((byte)cmd_channel==0xFF&&(byte)first==0x55&&(byte)second==0x55){ //时间
        int current=millis()-TIME;
        Serial.write((byte)cmd_channel);
        Serial.write((byte)(current&0x7F));
        Serial.write((byte)((current>>7)&0x7F));
      }
      else if((byte)cmd_channel==0xF9&&(byte)first==0x55&&(byte)second==0x55){  //学号
        int stu_num=8062;
        Serial.write((byte)cmd_channel);
        Serial.write((byte)(stu_num&0x7F));
        Serial.write((byte)((stu_num>>7)&0x7F));
      }      
    }
    else{ //无效
      Serial.write((byte)0xF);
      Serial.write((byte)0xA);
      Serial.write((byte)0x7F);
    }
  }
  else if(Serial.available()==2){
    int cmd_channel=Serial.read();
    int data=Serial.read();
    int Channel=cmd_channel&0xF;
    if((byte)(cmd_channel>>4)==0x9){
      pinMode(Channel, OUTPUT);
      if(data==0){
        digitalWrite(Channel,LOW);
      }
      else if(data==1){
        digitalWrite(Channel,HIGH);
      }
    }
    else if((byte)(cmd_channel>>4)==0xC&&(byte)data==0x66){
//      pinMode(Channel, INPUT);
      int state=digitalRead(Channel);
      Serial.write((byte)cmd_channel);
      Serial.write((byte)state);
    }
    else{ //无效
      Serial.write((byte)0xF);
      Serial.write((byte)0xA);
      Serial.write((byte)0x7F);
    }
  }
  delay(100);
}
