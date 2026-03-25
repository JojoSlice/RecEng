extension ErrorMessage on Object {
  String get message => toString().replaceFirst('Exception: ', '');
}
