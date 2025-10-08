# THMHJ3_StringPatcher

이 프로젝트는 동방막화제 영야편의 텍스트를 CSV 매핑 파일을 통해 수정하는 간단한 C# 기반 문자열 패처입니다.<br/>
게임 업데이트 시마다 exe가 바뀌더라도, 매핑 파일을 유지하면 내부 문자열을 한글화할 수 있습니다.

## Release 다운로드 시 사용 방법
1. 동방막화제 영야편 로컬 폴더에 압축 해제<br/>
2. publish 폴더 내부의 THMHJ3_StringPatcher.exe 실행<br/>

📦 Content<br/>
📦 Replay<br/>
📦 THMHJ3_StringPatcher<br/>
 ┣ 📦 publish<br/>
  &emsp; ┣ 📄 THMHJ3_StringPatcher.exe   <-이 파일을 실행<br/>
  &emsp; ┗ 📄 THMHJ3_StringPatcher.pdb<br/> 
 ┣ 📄 avcodec-61.dll<br/>
 ┣ 📄 avformat-61.dll<br/>
 ...

 의 형태가 되어야 함.

## 직접 빌드 시 사용 방법
1. .NET SDK 8.0 이상 설치<br/>
2. 동방막화제 영야편 로컬 폴더에 압축 해제<br/>
3. Run.bat 실행<br/>
4. 이후 자동으로 프로그램까지 실행됩니다. 원하지 않으면 .bat 파일을 수정해주세요.

## 파일 설명
- **Program.cs**: 메인 코드<br/>
- **mapping.csv**: 원문과 번역문이 들어 있는 CSV<br/>
- **Run.bat**: 빌드 및 실행 자동화 스크립트

