import subprocess
import os

def run_unity_get_logs(project_path):
    unity_executable = 'C:\Program Files\Unity\Hub\Editor\2021.2.18f1\Editor\Unity.exe'
    command = f'"{unity_executable}" -batchmode -quit -projectPath "{project_path}" -logFile "D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\.unity-log.txt"'
    subprocess.run(command, shell=True)

if __name__ == '__main__':
    project_path = 'D:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022'
    run_unity_get_logs(project_path)