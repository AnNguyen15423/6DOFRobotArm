#include <NewPing.h>
#include <LiquidCrystal_I2C.h>
#include <Keypad.h>
#include <Arduino.h>
#include <ESP32Servo.h>

Servo ser1;
const byte ROWS = 4; //four rows
const byte COLS = 4; //four columns
char hexaKeys[ROWS][COLS] = {
  {'1','2','3','A'},
  {'4','5','6','B'},
  {'7','8','9','C'},
  {'*','0','#','D'}
};
byte rowPins[ROWS] = {15, 2, 0, 4};
byte colPins[COLS] = {16, 17, 5, 18};

Keypad customKeypad = Keypad(makeKeymap(hexaKeys), rowPins, colPins, ROWS, COLS); 
LiquidCrystal_I2C lcd(0x27, 20, 4);

#define TRIGGER_PIN  12 
#define ECHO_PIN     14  
#define MAX_DISTANCE 400 
#define SERVO 27

NewPing sonar(TRIGGER_PIN, ECHO_PIN, MAX_DISTANCE); 
float tempval1;
float tempval2;
int finalval;

char password[5] = "0123"; 
static char pass[5]; 
static int passIndex = 0;
bool firstTime = true; 
unsigned long openTime = 99999999999; 
unsigned long timeNow = 99999999999; 
int servoAngle = 0; 
bool messageDisplayed = false; 
bool changePassword = false;

void setup() {
  Serial.begin(9600); 
  lcd.init();
  lcd.backlight();
  lcd.clear();
  ser1.attach(SERVO);
}

void loop() {
  timeNow = millis();
  updateDistance();

  if (finalval < 40) {
    if (firstTime) {
      displayInitialMessage();
    } else {
      PASSWORDS();
    }
  } else {
    if (!messageDisplayed) {
      displayGoodDayMessage();
    }
    firstTime = true;

  }

  Serial.print(timeNow);
  Serial.print("\t");
  Serial.print(openTime);

}

void updateDistance() {
  delay(20);                     
  Serial.print("Ping: ");
  int iterations = 10;
  tempval1 = ((sonar.ping_median(iterations) / 2) * 0.0343);
  if (tempval1 - tempval2 > 60 || tempval1 - tempval2 < -60) {
    tempval2 = (tempval1 * 0.02) + (tempval2 * 0.98);
  } else {
    tempval2 = (tempval1 * 0.4) + (tempval2 * 0.6);
  }
  finalval = tempval2;  

  Serial.print(finalval);
  Serial.println("cm");
}

void displayInitialMessage() {
  lcd.clear();
  lcd.setCursor(0,0);
  lcd.print("   FILL YOUR");
  lcd.setCursor(0,1);
  lcd.print("PASSWORDS HERE:");
  firstTime = false;
  messageDisplayed = false;
}

void displayGoodDayMessage() {
  lcd.clear();
  lcd.setCursor(0,0);
  lcd.print("HAVE A GOOD DAY!");
  lcd.setCursor(0,1);
  lcd.print("ENJOY YOUR DAY!");
  messageDisplayed = true;
}

void PASSWORDS() {
  char customKey = customKeypad.getKey();

  if (customKey) {
    if (customKey == 'A') {
      deletePreviousCharacter();
    } else if (customKey == 'B') {
      deleteAllCharacters();
    } else if (customKey == 'C') {
      changePasswordRequested();
    } else {
      enterPassword(customKey);
    }
  }

  if (passIndex == 4) {
    verifyPassword();
  }
}

void deletePreviousCharacter() {
  if (passIndex > 0) {
    passIndex--;
    pass[passIndex] = '\0';
    updatePasswordDisplay();
  }
}

void deleteAllCharacters() {
  passIndex = 0;
  pass[0] = '\0';
  updatePasswordDisplay();
}

void changePasswordRequested() {
  changePassword = true;
  passIndex = 0;
  pass[passIndex] = '\0';
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("ENTER NEW PASSWORD:");
  lcd.setCursor(0, 1);
  lcd.print(" (4 digits)");
}

void enterPassword(char customKey) {
  if (changePassword) {
    if (passIndex < 4) {
      pass[passIndex++] = customKey;
      pass[passIndex] = '\0';
      updatePasswordDisplay();
    }
    if (passIndex == 4) {
      changePassword = false;
      lcd.clear();
      lcd.setCursor(0, 0);
      lcd.print("PASSWORD CHANGED!");
      delay(500);
      lcd.clear();
      lcd.setCursor(0, 0);
      lcd.print("YOUR PASSWORDS:");
      lcd.setCursor(0, 1);
      lcd.print(pass);
      strcpy(password, pass);
    }
  } else {
    if (passIndex < 4) {
      pass[passIndex++] = customKey;
      pass[passIndex] = '\0';
      updatePasswordDisplay();
    }
  }
}

void updatePasswordDisplay() {
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("YOUR PASSWORDS:");
  lcd.setCursor(0, 1);
  lcd.print(pass);
}


void verifyPassword() {
  if (strcmp(pass, password) == 0) {
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print("WELCOME BACK SIR");
    delay(1000);
    lcd.clear();
    lcd.setCursor(2, 0);
    lcd.print("GLAD TO SEE");
    lcd.setCursor(3, 1);
    lcd.print("YOU AGAIN!");
    passIndex = 0;
    pass[0] = '\0';
    ser1.write(90);

    servoAngle = 90;

    delay(5000);
    ser1.write(0);
    servoAngle = 0;

  } else {
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print(" WRONG PASSWORDS");
    lcd.setCursor(0, 1);
    lcd.print("   TRY AGAIN!");
    passIndex = 0;
    pass[0] = '\0';
  }
}