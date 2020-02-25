#!/bin/sh
#
# Usage: gputest.sh
# Change job name and email address as needed 
#        

# -- our name ---
#$ -N DeckSearch
#$ -S /bin/sh
# Make sure that the .e and .o file arrive in the
#working directory
#$ -cwd
#Merge the standard out and standard error to one file
#$ -j y
# Send mail at submission and completion of script
# Specify GPU queue
#$ -q medium
#$ -l mem_free=14.0G
/bin/echo Running on host: `hostname`.
/bin/echo In directory: `pwd`
/bin/echo Starting on: `date`

# Load dotnet module
module load dotnet/2.2.300
#Full path to executable
dotnet bin/DeckSearch.dll config/paladin.tml
