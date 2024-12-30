#include <AccelStepper.h>
#include <Servo.h>

// Define stepper motor pins

const int stepperDirPins[] = {12,8,6,4,2,18};
const int stepperStepPins[] = {15,9,7,5,3,19};


// Define limit switch pins
const int limitSwitchPins[] = {37, 36, 35, 34, 33, 32};

// Direction to reach the limit switch for each joint
const int directions[] = {-1, 1, -1, -1, 1, 1};

// Steps per revolution for each joint
const long revolutions[] = {64000, 64000, 54000, 40000, 6000, 6600};

// Home positions in degrees for each joint
const int homePositions[] = {-59, 178, -20, -3, 200, 180};

// Servo pin
const int SERVO_PIN = 14;

// Servo object
Servo servo;

String cmd,command,commands;
float value;
String Process[6];

int index=0;

bool isHeld = false;

bool Auto = true;
bool Man = false;
bool Simple = true;
bool Detailed = false;
bool startProcess = false;
bool stopAllMotors = true;

// Array of stepper motors
AccelStepper steppers[] = {
  AccelStepper(AccelStepper::DRIVER, stepperStepPins[0], stepperDirPins[0]),
  AccelStepper(AccelStepper::DRIVER, stepperStepPins[1], stepperDirPins[1]),
  AccelStepper(AccelStepper::DRIVER, stepperStepPins[2], stepperDirPins[2]),
  AccelStepper(AccelStepper::DRIVER, stepperStepPins[3], stepperDirPins[3]),
  AccelStepper(AccelStepper::DRIVER, stepperStepPins[4], stepperDirPins[4]),
  AccelStepper(AccelStepper::DRIVER, stepperStepPins[5], stepperDirPins[5])
};
long int jointRotate(float angle, long int revolution) {
  return (angle / 360) * revolution;
}
void Angle(float value, int i) {
  if (i >= 0 && i < 6) { // Check valid index
    long targetSteps = jointRotate(value, revolutions[i]);
    steppers[i].moveTo(targetSteps);
    steppers[i].runToPosition();
  } else {
    Serial.println("Invalid stepper index.");
  }
}
void Gripper(int state){
  if(state == 1){
    if (!isHeld) {
      for (int i = servo.read(); i >= 51; i--) {
        servo.write(i);
        delay(15);
      }
      isHeld = true;
    }
  }
  else{
    if (isHeld) {
      for (int i = servo.read(); i <= 70; i++) {
        servo.write(i);
        delay(15);
      }
      isHeld = false;
    }
  }
}
// void homing(AccelStepper &stepper, int limitSwitchPin, int direction, int stepsPerDegree) {
//   bool homed = false;
//   stepper.setCurrentPosition(0);

//   while (!homed) {
//     int limitState = digitalRead(limitSwitchPin);

//     if (limitState == HIGH) {
//       homed = true; // Stop homing when limit switch is triggered
//     } else {
//       stepper.moveTo(stepper.currentPosition() + direction * stepsPerDegree);
//       stepper.runToPosition();
//     }
//   }
// }
bool readStableSwitchState(int pin, int stableReadCount = 5) {
  int countHigh = 0;
  for (int i = 0; i < stableReadCount; i++) {
    if (digitalRead(pin) == HIGH) {
      countHigh++;
    }
    delay(10); // Tạm dừng để ổn định tín hiệu
  }
  return countHigh == stableReadCount;
}

void homing(AccelStepper &stepper, int limitSwitchPin, int direction, int stepsPerDegree) {
  bool homed = false;
  stepper.setCurrentPosition(0);

  while (!homed) {
    if (readStableSwitchState(limitSwitchPin)) {
      homed = true; // Stop homing khi công tắc hành trình ổn định ở mức HIGH
    } else {
      stepper.moveTo(stepper.currentPosition() + direction * stepsPerDegree);
      stepper.runToPosition();
    }
  }
}


void homeAllJoints() {
  const int homingOrder[] = {1,0,3,2,5,4};
  for (int i = 0; i < 6; i++) {
    int jointIndex = homingOrder[i];
    if (jointIndex == 3) {
      int state = digitalRead(limitSwitchPins[3]);
      if(state!=1){
        steppers[2].setCurrentPosition(0);
        Angle(30,2);
      }
    }
    homing(steppers[jointIndex], limitSwitchPins[jointIndex], directions[jointIndex], revolutions[jointIndex] / 360);
    steppers[jointIndex].setCurrentPosition(jointRotate(homePositions[jointIndex], revolutions[jointIndex]));
  }
}
void waitingPosition(){
  const int movingOrder[] = {1,0,2,4,3,5};
  const int angleOrder[] = {90,65,-7,90,90,90};
  for(int i=0;i<6;i++){
    int jointIndex = movingOrder[i];
    Angle(angleOrder[jointIndex],jointIndex);
  }
}
void waitingPosition2(){
  const int movingOrder[] = {1,5,4,0,2,3};
  const int angleOrder[] = {0,65,-7,60,0,-90};
  for(int i=0;i<6;i++){
    int jointIndex = movingOrder[i];
    Angle(angleOrder[jointIndex],jointIndex);
  }
}
void P1(){
  const int movingOrder[] = {3,2,4,5,0,1};
  const int angleOrder[] = {114,57,-18,90,130,110};
  for(int i=0;i<6;i++){
    int jointIndex = movingOrder[i];
    Angle(angleOrder[jointIndex],jointIndex);
  }
}
void P2(){
  const int movingOrder[] = {3,2,4,5,0,1};
  const int angleOrder[] = {85,57,-18,90,130,80};
  for(int i=0;i<6;i++){
    int jointIndex = movingOrder[i];
    Angle(angleOrder[jointIndex],jointIndex);
  }
}
void P3(){
  const int movingOrder[] = {3,2,4,5,0,1};
  const int angleOrder[] = {63,49,3,83,140,60};
  for(int i=0;i<6;i++){
    int jointIndex = movingOrder[i];
    Angle(angleOrder[jointIndex],jointIndex);
  }
}
void PA(){
  const int movingOrder[] = {4,5,3,2,0,1};
  const int angleOrder[] = {-38,42,-35,40,14,-80};
  for(int i=0;i<6;i++){
    int jointIndex = movingOrder[i];
    Angle(angleOrder[jointIndex],jointIndex);
  }
}
void PB(){
  const int movingOrder[] = {4,5,3,2,0,1};
  const int angleOrder[] = {-9,42,-35,40,14,-80};
  for(int i=0;i<6;i++){
    int jointIndex = movingOrder[i];
    Angle(angleOrder[jointIndex],jointIndex);
  }
}
void PC(){
  const int movingOrder[] = {4,5,3,2,0,1};
  const int angleOrder[] = {37,40,-22,70,24,-78};
  for(int i=0;i<6;i++){
    int jointIndex = movingOrder[i];
    Angle(angleOrder[jointIndex],jointIndex);
  }
}
void manMode(){
  if(Simple){
    if (command == "H") {
      homeAllJoints();
    } 
    else if (command == "G") {
      Gripper(1);
    } 
    else if (command == "D") {
      Gripper(0);
    }
    else if(command == "P1"){
      waitingPosition();
      P1();
    }
    else if(command == "P2"){
      waitingPosition();
      P2();
    }
    else if(command == "P3"){
      waitingPosition();
      P3();
    }
    else if(command == "PA"){
      waitingPosition2();
      PA();
    }
    else if(command == "PB"){
      waitingPosition2();
      PB();
    }
    else if(command == "PC"){
      waitingPosition2();
      PC();
    }
  }

  else if(Detailed){
    if (command == "1") {
      Angle(value, 0); // Stepper 1
    } 
    else if (command == "2") {
      Angle(value, 1); // Stepper 2
    } 
    else if (command == "3") {
      Angle(value, 2); // Stepper 3
    } 
    else if (command == "4") {
      Angle(value, 3); // Stepper 4
    } 
    else if (command == "5") {
      Angle(value, 4); // Stepper 5
    } 
    else if (command == "6") {
      Angle(value, 5); // Stepper 6
    }
  }
}
void autoMode(){
  if (Process[index] == "Pos1") {
    waitingPosition();
    P1();
    if (index % 2 == 0) {
      Gripper(1); // Đóng gripper
    } else {
      Gripper(0); // Mở gripper
    }
  } 
  else if (Process[index] == "Pos2") {
    waitingPosition();
    P2();
    if (index % 2 == 0) {
      Gripper(1); // Đóng gripper
    } else {
      Gripper(0); // Mở gripper
    }
  } 
  else if (Process[index] == "Pos3") {
    waitingPosition();
    P3();
    if (index % 2 == 0) {
      Gripper(1); // Đóng gripper
    } else {
      Gripper(0); // Mở gripper
    }
  } 
  else if (Process[index] == "PosA") {
    waitingPosition2();
    PA();
    if (index % 2 == 0) {
      Gripper(1); // Đóng gripper
    } else {
      Gripper(0); // Mở gripper
    }
  } 
  else if (Process[index] == "PosB") {
    waitingPosition2();
    PB();
    if (index % 2 == 0) {
      Gripper(1); // Đóng gripper
    } else {
      Gripper(0); // Mở gripper
    }
  } 
  else if (Process[index] == "PosC") {
    waitingPosition2();
    PC();
    if (index % 2 == 0) {
      Gripper(1); // Đóng gripper
    } else {
      Gripper(0); // Mở gripper
    }
  }
}
void setup() {
  Serial.begin(9600);

  // Initialize stepper motors and limit switches
  for (int i = 0; i < 6; i++) {
    if(i<4&&i!=2){
      steppers[i].setMaxSpeed(12000);
      steppers[i].setAcceleration(6000);
    }
    else{
      steppers[i].setMaxSpeed(6000);
      steppers[i].setAcceleration(3000);
    }

    pinMode(limitSwitchPins[i], INPUT_PULLUP);
  }
  // Initialize servo motor
  servo.attach(SERVO_PIN);
  servo.write(70); // Set servo to neutral position
  delay(1000);
  homeAllJoints(); 

}
void loop() {
  cmd = Serial.readStringUntil('\n');
  cmd.trim();
  command = cmd.substring(0, cmd.indexOf(':'));
  value = cmd.substring(cmd.indexOf(':') + 1).toFloat();
  //command = "Start/Pos1/PosA/Pos2/PosB/Pos3/PosC/"; // Giả lập dữ liệu nhận
  if (command.startsWith("Start")) {
    stopAllMotors = false;    
    for (int i = 0; i < 6; i++) {
      Process[i] = ""; 
    }

    String commands = command.substring(command.indexOf('/') + 1);
    int count = 0; // Số lệnh hợp lệ đã thêm vào Process[]

    // Tách từng lệnh ngăn cách bởi "/"
    while (commands.length() > 0 && count <6) {
      int slashIndex = commands.indexOf('/');

      // Nếu không còn "/", lấy phần cuối
      if (slashIndex == -1) {
        String tempCommand = commands; // Lệnh cuối cùng
        if (tempCommand.startsWith("Pos")) { // Kiểm tra tính hợp lệ
          Process[count++] = tempCommand;
        }
        break; // Thoát vòng lặp
      } 
      else {
        // Lấy lệnh trước "/"
        String tempCommand = commands.substring(0, slashIndex);
        if (tempCommand.startsWith("Pos")) { // Chỉ nhận các lệnh bắt đầu bằng "Pos"
          Process[count++] = tempCommand;
        }
        // Cắt chuỗi còn lại
        commands = commands.substring(slashIndex + 1);
      }
    }
  }

  if(command=="Auto"){
    Auto=true;
    Man=false;
  }

  else if(command=="Man"){
    Auto=false;
    Man=true;
  }

  else if(command=="Simple"){
    Simple = true;
    Detailed = false;
  }

  else if(command=="Detailed"){
    Simple=false;
    Detailed=true;
  }

  else if (command == "Pause") {
    stopAllMotors = true;
    for(int i;i<6;i++){
      Process[i]="";
    }
    index=0;
  }

  if(Man){
    manMode();
  }

  if(Auto){
    //autoMode();
    if(stopAllMotors==false){
      if(index<6){
        Serial.println(Process[index]);
        autoMode();
        index++;
      }
    }
  }
}
