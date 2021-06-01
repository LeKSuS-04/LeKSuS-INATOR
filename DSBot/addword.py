### Script used to manualy add words to wordlist

with open('wordlist.txt', 'r') as f:
    words = f.read().split('\n')

while True:
    command = input(' > ')

    if command == '_quit':
        break
    elif command == '_delete':
        print(f'{words[-1]} is deleted from the wordlist')
        words.remove(words[-1])
    elif command == '_length':
        print(f'wordlist contains {len(words)} words at the moment with the last one {words[-1]}')
    else:
        if command not in words:
            words.append(command)
        else:
            print(f'{command} is already in the wordlist')

words.sort()

with open('wordlist.txt', 'w') as f:
    f.write("\n".join(words))
