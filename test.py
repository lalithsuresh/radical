from random import Random 

r = Random(1) # same seed

clients = []

f = open("test.txt", 'w')

f.write ("connect central-1 123.123.123.123:1234\n")
f.write ("connect central-2 123.123.123.123:1234\n")
f.write ("connect central-3 123.123.123.123:1234\n")

def generate(posts):
    reservation_count = 0

    for x in range(posts): 
        client = "user" + str(x)
        clients.append(client)
        f.write("connect " + client + " 127.0.0.1:1234\n")

    # generate reservation requests
    for x in range(posts):
        reservation_count = reservation_count + 1
        no = r.randint(1,posts-1)
        s = "reservation {" + str(x) + "; "

        taken = []
        first = r.choice(clients)
        clients.remove(first)

        s = s + first + ","

        for y in range(no):
            client = r.choice(clients)
            taken.append(client)
            clients.remove(client)
            s = s + client + ","
            reservation_count = reservation_count + 1

        s = s[:-1] + "; " + str(x) + " }"

        # put them back
        clients.append(first)
        for c in taken:
            clients.append(c)

        taken = []

        f.write(s + "\n")
    
    f.close()

    print str(reservation_count) + " reservations created for " + str(posts) + " clients"

if __name__ == "__main__":
    generate(10)
