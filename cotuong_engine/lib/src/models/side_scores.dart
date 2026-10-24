class SideScores {
  int h1;
  int h2;
  int h3;
  int h4;

  SideScores({
    this.h1 = 0,
    this.h2 = 0,
    this.h3 = 0,
    this.h4 = 0,
  });

  int get total => h1 + h2 + h3 + h4;
}
